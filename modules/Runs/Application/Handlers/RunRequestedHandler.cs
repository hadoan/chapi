using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Runs.Application.Contracts.Events;
using Runs.Domain;
using RunPack.Domain;
using ShipMvp.Domain.Files;
using ShipMvp.Core.EventBus;
using Runs.Application.Constants;

namespace Runs.Application.Handlers;

// Additional event types for run execution progress and completion
public record RunProgressEto(Guid RunId, int Index, string Name, string Status, string ArtifactPrefix);
public record RunCompletedEto(Guid RunId, int Passed, int Failed, string SummaryPath);
public record RunFailedEto(Guid RunId, string Reason);

public class RunRequestedHandler : IDistributedEventHandler<RunRequestedEto>
{
    private readonly IRunRepository _runs;
    private readonly IRunPackRepository _runPackRepo;
    private readonly IFileStorageService _fs;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<RunRequestedHandler> _log;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public RunRequestedHandler(
        IRunRepository runs,
        IRunPackRepository runPackRepo,
        IFileStorageService fs,
        IHttpClientFactory http,
        ILogger<RunRequestedHandler> log)
    {
        _runs = runs;
        _runPackRepo = runPackRepo;
        _fs = fs;
        _http = http;
        _log = log;
    }

    public async Task HandleAsync(RunRequestedEto eto)
    {
        var run = await _runs.GetByIdAsync(eto.RunId);
        if (run is null)
        {
            _log.LogWarning("Run {RunId} not found; dropping.", eto.RunId);
            return;
        }

        // Idempotency check using run status
        if (run.Status == RunStatus.Running || run.Status == RunStatus.Passed || run.Status == RunStatus.Failed)
        {
            _log.LogInformation("Run {RunId} already {Status}; dropping duplicate message.", run.Id, run.Status);
            return;
        }

        run.MarkRunning(); // This sets status to Running and StartedAt
        await _runs.UpdateAsync(run);

        try
        {
            // 1) Load IR - handle both regular paths and runpack references
            Stream suiteStream;
            if (eto.IrPath.StartsWith(RunConstants.Schemes.RunPackScheme))
            {
                // Extract runpack ID from path like: runpack://guid/tests.json
                var pathParts = eto.IrPath.Replace(RunConstants.Schemes.RunPackScheme, "").Split('/');
                var runPackId = Guid.Parse(pathParts[0]);

                var runPack = await _runPackRepo.Query()
                    .Include(rp => rp.Files)
                    .ThenInclude(rpf => rpf.File)
                    .FirstOrDefaultAsync(rp => rp.Id == runPackId);

                if (runPack == null)
                    throw new ArgumentException(string.Format(RunConstants.Messages.RunPackNotFoundMessage, runPackId));

                var testFile = runPack.Files
                    .FirstOrDefault(rpf => rpf.File != null &&
                                         rpf.File.FileName.Equals(RunConstants.Storage.TestsFileName, StringComparison.OrdinalIgnoreCase));

                if (testFile?.File == null)
                    throw new InvalidOperationException(RunConstants.Messages.TestsFileNotFoundMessage);

                suiteStream = await _fs.DownloadAsync(
                    testFile.File.ContainerName,
                    testFile.File.StoragePath);
            }
            else
            {
                // Regular IR path - use the same container as FileRunIrStorage
                suiteStream = await _fs.DownloadAsync(RunConstants.Storage.ArtifactsContainer, eto.IrPath);
            }

            SimpleSuite suite;
            using (suiteStream)
            {
                suite = await JsonSerializer.DeserializeAsync<SimpleSuite>(suiteStream, Json)
                        ?? throw new InvalidOperationException(RunConstants.Messages.InvalidIrMessage);
            }

            // 2) Execute tests
            var client = _http.CreateClient(RunConstants.Http.ExecutorClientName);
            // TODO: Set BaseAddress from environment/project configuration if needed
            // if (!string.IsNullOrWhiteSpace(run.BaseUrl))
            //     client.BaseAddress = new Uri(run.BaseUrl!, UriKind.Absolute);

            var results = new List<TestResult>();
            var pass = 0;
            var fail = 0;

            for (var i = 0; i < suite.Tests.Count; i++)
            {
                var t = suite.Tests[i];
                var name = $"{t.Method} {t.Endpoint}";
                var artPrefix = string.Format(RunConstants.Paths.ArtifactPrefixTemplate, run.Id, i, SanitizeFile(name));

                // TODO: Publish progress events if event bus is available
                // await _bus.PublishAsync(new RunProgressEto(run.Id, i, name, "started", artPrefix));

                var request = new HttpRequestMessage(new HttpMethod(t.Method), t.Endpoint);

                // Headers
                if (t.Headers is { Count: > 0 })
                {
                    foreach (var kv in t.Headers)
                    {
                        if (kv.Key.Equals(RunConstants.Http.ContentTypeHeader, StringComparison.OrdinalIgnoreCase))
                            request.Content ??= new StringContent(t.Body ?? string.Empty, Encoding.UTF8, kv.Value);
                        else
                            request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                    }
                }

                // Accept JSON if hinted
                if (t.AcceptJson == true && !request.Headers.Accept.Any())
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(RunConstants.Http.JsonContentType));

                // TODO: Add authentication from project/environment settings
                // if (!string.IsNullOrWhiteSpace(run.Token))
                //     request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", run.Token);

                // Body
                if (!string.IsNullOrEmpty(t.Body) && request.Content is null)
                    request.Content = new StringContent(t.Body, Encoding.UTF8,
                        t.ContentType ?? RunConstants.Http.JsonContentType);

                // Save request artifact
                await _fs.UploadAsync(
                    RunConstants.Storage.ArtifactsContainer,
                    $"{artPrefix}{RunConstants.Storage.RequestFileSuffix}",
                    new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                    {
                        name,
                        url = client.BaseAddress is null ? t.Endpoint : new Uri(client.BaseAddress, t.Endpoint).ToString(),
                        method = t.Method,
                        headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                        contentHeaders = request.Content?.Headers?.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                        body = t.Body
                    }, Json))),
                    RunConstants.Http.JsonContentType);

                // Send
                var startedAt = DateTimeOffset.UtcNow;
                HttpResponseMessage resp;
                try
                {
                    resp = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                }
                catch (Exception ex)
                {
                    // Transport failure counts as test failure; persist error
                    fail++;
                    var err = new TestResult(name, false, 0, null, null, ex.Message, (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);
                    results.Add(err);

                    await _fs.UploadAsync(
                        RunConstants.Storage.ArtifactsContainer,
                        $"{artPrefix}{RunConstants.Storage.ResponseFileSuffix}",
                        new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(err, Json))),
                        RunConstants.Http.JsonContentType);

                    // TODO: Publish progress events if event bus is available
                    // await _bus.PublishAsync(new RunProgressEto(run.Id, i, name, "failed", artPrefix));
                    continue;
                }

                var bodyText = await resp.Content.ReadAsStringAsync();
                var ct = resp.Content.Headers.ContentType?.ToString();
                var statusOk = MatchesExpected(resp.StatusCode, t.ExpectedStatus ?? RunConstants.Http.DefaultExpectedStatus);
                var jsonOk = !(t.AcceptJson == true) || (ct?.Contains("json", StringComparison.OrdinalIgnoreCase) == true);
                var ok = statusOk && jsonOk;

                if (ok) pass++; else fail++;

                var result = new TestResult(
                    name, ok, (int)resp.StatusCode, ct,
                    resp.Headers.Concat(resp.Content.Headers)
                        .ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                    bodyText, (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);

                results.Add(result);

                await _fs.UploadAsync(
                    RunConstants.Storage.ArtifactsContainer,
                    $"{artPrefix}{RunConstants.Storage.ResponseFileSuffix}",
                    new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result, Json))),
                    RunConstants.Http.JsonContentType);

                // TODO: Publish progress events if event bus is available
                // await _bus.PublishAsync(new RunProgressEto(run.Id, i, name, ok ? "passed" : "failed", artPrefix));
            }

            // 3) Persist summary + mark run
            var summaryPath = string.Format(RunConstants.Paths.SummaryPathTemplate, run.Id);
            await _fs.UploadAsync(
                RunConstants.Storage.ArtifactsContainer,
                summaryPath,
                new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                {
                    runId = run.Id,
                    startedAt = run.StartedAt,
                    completedAt = DateTimeOffset.UtcNow,
                    total = results.Count,
                    passed = pass,
                    failed = fail,
                    results
                }, Json))),
                RunConstants.Http.JsonContentType);

            // Update run status using domain methods
            if (fail == 0)
            {
                run.Complete(true);
            }
            else
            {
                run.Complete(false, string.Format(RunConstants.Messages.TestsFailedMessage, fail));
            }

            await _runs.UpdateAsync(run);

            // TODO: Emit completion events if event bus is available
            // if (fail == 0)
            //     await _bus.PublishAsync(new RunCompletedEto(run.Id, pass, fail, summaryPath));
            // else
            //     await _bus.PublishAsync(new RunFailedEto(run.Id, $"One or more tests failed ({fail})."));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Run {RunId} failed.", eto.RunId);
            run.Complete(false, ex.Message);
            await _runs.UpdateAsync(run);

            // TODO: Emit failure events if event bus is available
            // await _bus.PublishAsync(new RunFailedEto(run.Id, ex.Message));
            throw; // let message queue retry based on subscription policy
        }
    }

    private static bool MatchesExpected(System.Net.HttpStatusCode code, string expected)
    {
        var numeric = (int)code;
        if (expected.EndsWith(RunConstants.Http.StatusWildcardSuffix, StringComparison.Ordinal))
        {
            var hundred = (expected[0] - '0') * 100;
            return numeric >= hundred && numeric < hundred + 100;
        }
        if (int.TryParse(expected, out var exact))
            return numeric == exact;
        // fallback
        return numeric is >= 200 and < 300;
    }

    private static string SanitizeFile(string s)
    {
        var bad = Path.GetInvalidFileNameChars();
        return new string(s.Select(ch => bad.Contains(ch) ? RunConstants.FileSystem.InvalidCharReplacement : ch).ToArray());
    }

    // --- IR models (simple) ---

    public class SimpleSuite
    {
        [JsonPropertyName("tests")] public List<SimpleTest> Tests { get; set; } = new();
    }

    public class SimpleTest
    {
        [JsonPropertyName("endpoint")] public string Endpoint { get; set; } = default!;
        [JsonPropertyName("method")] public string Method { get; set; } = RunConstants.Http.DefaultMethod;
        [JsonPropertyName("expected_status")] public string? ExpectedStatus { get; set; } = RunConstants.Http.DefaultExpectedStatus;
        [JsonPropertyName("headers")] public Dictionary<string, string>? Headers { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("content_type")] public string? ContentType { get; set; }
        [JsonPropertyName("accept_json")] public bool? AcceptJson { get; set; } // derived hint
    }

    public record TestResult(
        string Name,
        bool Ok,
        int Status,
        string? ContentType,
        Dictionary<string, string>? Headers,
        string? Body,
        double DurationMs
    );
}