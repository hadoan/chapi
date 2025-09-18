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
using Environments.Domain;

namespace Runs.Application.Handlers;

// Additional event types for run execution progress and completion
public record RunProgressEto(Guid RunId, int Index, string Name, string Status, string ArtifactPrefix);
public record RunCompletedEto(Guid RunId, int Passed, int Failed, string SummaryPath);
public record RunFailedEto(Guid RunId, string Reason);

public class RunRequestedHandler : IDistributedEventHandler<RunRequestedEto>
{
    private readonly IRunRepository _runs;
    private readonly IRunPackRepository _runPackRepo;
    private readonly IEnvironmentRepository _environmentRepo;
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
        IEnvironmentRepository environmentRepo,
        IFileStorageService fs,
        IHttpClientFactory http,
        ILogger<RunRequestedHandler> log)
    {
        _runs = runs;
        _runPackRepo = runPackRepo;
        _environmentRepo = environmentRepo;
        _fs = fs;
        _http = http;
        _log = log;
    }

    public async Task HandleAsync(RunRequestedEto eto)
    {
        var run = await ValidateAndPrepareRun(eto);
        if (run == null) return;

        try
        {
            var baseUrl = await GetEnvironmentBaseUrl(eto.EnvironmentId);
            var suite = await LoadAndParseTestSuite(eto.IrPath, run.Id);
            var results = await ExecuteTests(suite, baseUrl, run.Id);
            await PersistResults(results, run);
        }
        catch (Exception ex)
        {
            await HandleRunFailure(run, ex, eto.RunId);
            throw; // let message queue retry based on subscription policy
        }
    }

    private async Task<Run?> ValidateAndPrepareRun(RunRequestedEto eto)
    {
        var run = await _runs.GetByIdAsync(eto.RunId);
        if (run is null)
        {
            _log.LogWarning("Run {RunId} not found; dropping.", eto.RunId);
            return null;
        }

        // Idempotency check using run status
        if (run.Status == RunStatus.Running || run.Status == RunStatus.Passed || run.Status == RunStatus.Failed)
        {
            _log.LogInformation("Run {RunId} already {Status}; dropping duplicate message.", run.Id, run.Status);
            return null;
        }

        run.MarkRunning(); // This sets status to Running and StartedAt
        await _runs.UpdateAsync(run);
        return run;
    }

    private async Task<string?> GetEnvironmentBaseUrl(Guid? environmentId)
    {
        if (!environmentId.HasValue) return null;

        var environment = await _environmentRepo.GetByIdAsync(environmentId.Value);
        if (environment != null)
        {
            _log.LogInformation("Using environment {EnvironmentId} with BaseUrl: {BaseUrl}", 
                environmentId, environment.BaseUrl);
            return environment.BaseUrl;
        }
        
        _log.LogWarning("Environment {EnvironmentId} not found", environmentId);
        return null;
    }

    private async Task<SimpleSuite> LoadAndParseTestSuite(string irPath, Guid runId)
    {
        var suiteStream = await LoadTestSuiteStream(irPath, runId);
        return await ParseTestSuiteFromStream(suiteStream, runId);
    }

    private async Task<Stream> LoadTestSuiteStream(string irPath, Guid runId)
    {
        if (irPath.StartsWith(RunConstants.Schemes.RunPackScheme))
        {
            return await LoadFromRunPack(irPath, runId);
        }
        else
        {
            return await LoadFromArtifactStorage(irPath, runId);
        }
    }

    private async Task<Stream> LoadFromRunPack(string irPath, Guid runId)
    {
        _log.LogInformation("Run {RunId} loading IR from RunPack reference: {IrPath}", runId, irPath);

        // Extract runpack ID from path like: runpack://guid/tests.json
        var pathParts = irPath.Replace(RunConstants.Schemes.RunPackScheme, "").Split('/');
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

        _log.LogInformation("Found tests file in RunPack {RunPackId}: Container={Container}, StoragePath={StoragePath}", 
            runPackId, testFile.File.ContainerName, testFile.File.StoragePath);

        await ValidateFileExists(testFile.File.ContainerName, testFile.File.StoragePath);
        return await _fs.DownloadAsync(testFile.File.ContainerName, testFile.File.StoragePath);
    }

    private async Task<Stream> LoadFromArtifactStorage(string irPath, Guid runId)
    {
        _log.LogInformation("Run {RunId} loading IR from artifact path: {IrPath}", runId, irPath);
        
        await ValidateFileExists(RunConstants.Storage.ArtifactsContainer, irPath);
        return await _fs.DownloadAsync(RunConstants.Storage.ArtifactsContainer, irPath);
    }

    private async Task ValidateFileExists(string container, string path)
    {
        try
        {
            var exists = await _fs.ExistsAsync(container, path);
            _log.LogInformation("Exists check for {Container}/{Path}: {Exists}", container, path, exists);
            if (!exists)
            {
                _log.LogWarning("Requested IR object not found in storage: {Container}/{Path}", container, path);
                throw new FileNotFoundException($"IR object not found: {container}/{path}");
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed checking existence for {Container}/{Path}", container, path);
        }
    }

    private async Task<SimpleSuite> ParseTestSuiteFromStream(Stream suiteStream, Guid runId)
    {
        using (suiteStream)
        {
            var content = await ReadStreamContent(suiteStream, runId);
            return await ParseJsonContent(content, runId);
        }
    }

    private async Task<string> ReadStreamContent(Stream stream, Guid runId)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        var length = ms.Length;
        _log.LogInformation("IR stream length for run {RunId}: {Length} bytes", runId, length);

        ms.Position = 0;
        using var sr = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var content = await sr.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(content))
        {
            _log.LogError("IR content is empty for run {RunId}. Will upload debug artifact and abort.", runId);
            await UploadDebugContent(runId, "");
            throw new InvalidOperationException("IR content is empty");
        }

        var preview = content.Length > 1000 ? content.Substring(0, 1000) : content;
        _log.LogDebug("IR content preview for run {RunId}: {Preview}", runId, preview);

        ValidateContentFormat(content, runId);
        return content;
    }

    private void ValidateContentFormat(string content, Guid runId)
    {
        var trimmed = content.TrimStart();
        
        if (trimmed.StartsWith("gs://", StringComparison.OrdinalIgnoreCase) || 
            trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var preview = content.Length > 1000 ? content.Substring(0, 1000) : content;
            _log.LogWarning("IR content for run {RunId} appears to contain a URL instead of JSON: {Preview}", runId, preview);
        }

        if (trimmed.Equals("NaN", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogError("IR content is literal 'NaN' for run {RunId}. This cannot be parsed as JSON.", runId);
            UploadDebugContent(runId, content).Wait();
            throw new InvalidOperationException("IR content invalid (NaN)");
        }
    }

    private async Task<SimpleSuite> ParseJsonContent(string content, Guid runId)
    {
        try
        {
            // First try the legacy format: top-level "tests" array
            if (JsonDocument.Parse(content).RootElement.TryGetProperty("tests", out var testsElement))
            {
                var suite = JsonSerializer.Deserialize<SimpleSuite>(testsElement.GetRawText(), Json) ?? new SimpleSuite();
                _log.LogInformation("Parsed legacy tests array; Count={Count}", suite.Tests.Count);
                return suite;
            }
            else
            {
                // Try chapi-ir format or fallback
                return await ParseChapiOrFallbackFormat(content, runId);
            }
        }
        catch (JsonException jex)
        {
            _log.LogError(jex, "Failed parsing IR JSON for run {RunId}. Content preview: {Preview}", 
                runId, content.Length > 1000 ? content.Substring(0, 1000) : content);
            await UploadDebugContent(runId, content);
            throw;
        }
    }

    private async Task<SimpleSuite> ParseChapiOrFallbackFormat(string content, Guid runId)
    {
        var doc = JsonDocument.Parse(content);
        if (doc.RootElement.TryGetProperty("suite", out var suiteElem) && 
            suiteElem.TryGetProperty("steps", out var stepsElem))
        {
            var tests = MapStepsToTests(stepsElem, runId);
            var suite = new SimpleSuite { Tests = tests };
            _log.LogInformation("Parsed chapi-ir suite.steps format; mapped tests Count={Count}", suite.Tests.Count);
            return suite;
        }
        else
        {
            // Fallback to legacy deserialization expecting full SimpleSuite
            var suite = JsonSerializer.Deserialize<SimpleSuite>(content, Json);
            _log.LogInformation("Parsed SimpleSuite directly; Count={Count}", suite?.Tests?.Count ?? 0);
            
            if (suite == null)
            {
                _log.LogError("Deserialized suite is null for run {RunId}. Uploading debug IR content.", runId);
                await UploadDebugContent(runId, content);
                throw new InvalidOperationException(RunConstants.Messages.InvalidIrMessage);
            }

            if (suite.Tests == null)
            {
                _log.LogError("Suite.Tests is null for run {RunId}. Uploading debug IR content and aborting.", runId);
                await UploadDebugContent(runId, content);
                throw new InvalidOperationException("IR missing 'tests' array");
            }
            
            return suite;
        }
    }

    private List<SimpleTest> MapStepsToTests(JsonElement stepsElem, Guid runId)
    {
        var tests = new List<SimpleTest>();
        foreach (var step in stepsElem.EnumerateArray())
        {
            try
            {
                var test = MapStepToTest(step);
                tests.Add(test);
            }
            catch (Exception mapEx)
            {
                _log.LogWarning(mapEx, "Failed mapping step to SimpleTest for run {RunId}", runId);
            }
        }
        return tests;
    }

    private SimpleTest MapStepToTest(JsonElement step)
    {
        string method = RunConstants.Http.DefaultMethod;
        string endpoint = string.Empty;
        string? expectedStatus = RunConstants.Http.DefaultExpectedStatus;
        Dictionary<string, string>? headers = null;
        string? body = null;
        string? contentType = null;
        bool? acceptJson = null;

        if (step.TryGetProperty("request", out var req))
        {
            if (req.TryGetProperty("method", out var m)) method = m.GetString() ?? method;
            if (req.TryGetProperty("url", out var u)) endpoint = u.GetString() ?? endpoint;
            if (req.TryGetProperty("uri", out var u2) && string.IsNullOrEmpty(endpoint)) endpoint = u2.GetString() ?? endpoint;
            if (req.TryGetProperty("body", out var b)) body = b.ValueKind == JsonValueKind.String ? b.GetString() : b.GetRawText();
            if (req.TryGetProperty("content_type", out var ct)) contentType = ct.GetString();
            if (req.TryGetProperty("headers", out var hdrs) && hdrs.ValueKind == JsonValueKind.Object)
            {
                headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in hdrs.EnumerateObject())
                    headers[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() ?? string.Empty : prop.Value.GetRawText();
            }
        }

        if (step.TryGetProperty("assert", out var assert))
        {
            if (assert.TryGetProperty("status", out var st))
            {
                if (st.ValueKind == JsonValueKind.Number && st.TryGetInt32(out var si)) expectedStatus = si.ToString();
                else if (st.ValueKind == JsonValueKind.String) expectedStatus = st.GetString();
            }
            // If assert contains body expectations, set acceptJson hint conservatively
            if (assert.TryGetProperty("body_contains", out var bc) && bc.ValueKind == JsonValueKind.Array)
                acceptJson = true;
        }

        return new SimpleTest
        {
            Method = method,
            Endpoint = endpoint,
            ExpectedStatus = expectedStatus,
            Headers = headers,
            Body = body,
            ContentType = contentType,
            AcceptJson = acceptJson
        };
    }

    private async Task UploadDebugContent(Guid runId, string content)
    {
        try
        {
            var debugStoragePath = $"debug/{runId}/ir-failed.json";
            await _fs.UploadAsync(RunConstants.Storage.ArtifactsContainer, debugStoragePath, 
                new MemoryStream(Encoding.UTF8.GetBytes(content)), RunConstants.Http.JsonContentType);
            _log.LogInformation("Uploaded debug IR to {Container}/{DebugPath}", 
                RunConstants.Storage.ArtifactsContainer, debugStoragePath);
        }
        catch (Exception upEx)
        {
            _log.LogWarning(upEx, "Failed to upload debug IR for run {RunId}", runId);
        }
    }

    private async Task<List<TestResult>> ExecuteTests(SimpleSuite suite, string? baseUrl, Guid runId)
    {
        var client = ConfigureHttpClient(baseUrl);
        var results = new List<TestResult>();

        for (var i = 0; i < suite.Tests.Count; i++)
        {
            var result = await ExecuteSingleTest(suite.Tests[i], client, baseUrl, runId, i);
            results.Add(result);
        }

        return results;
    }

    private HttpClient ConfigureHttpClient(string? baseUrl)
    {
        var client = _http.CreateClient(RunConstants.Http.ExecutorClientName);
        
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            _log.LogInformation("Set HTTP client BaseAddress to: {BaseUrl}", baseUrl);
        }

        return client;
    }

    private async Task<TestResult> ExecuteSingleTest(SimpleTest test, HttpClient client, string? baseUrl, Guid runId, int testIndex)
    {
        var endpoint = ProcessEndpoint(test.Endpoint, baseUrl);
        var name = $"{test.Method} {endpoint}";
        var artPrefix = string.Format(RunConstants.Paths.ArtifactPrefixTemplate, runId, testIndex, SanitizeFile(name));

        var request = BuildHttpRequest(test, endpoint);
        await SaveRequestArtifact(request, client, name, test, artPrefix);

        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            var resp = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            var result = await ProcessResponse(resp, test, name, startedAt);
            await SaveResponseArtifact(result, artPrefix);
            return result;
        }
        catch (Exception ex)
        {
            var errorResult = new TestResult(name, false, 0, null, null, ex.Message, 
                (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);
            await SaveResponseArtifact(errorResult, artPrefix);
            return errorResult;
        }
    }

    private string ProcessEndpoint(string originalEndpoint, string? baseUrl)
    {
        string endpoint = originalEndpoint;
        if (!string.IsNullOrWhiteSpace(baseUrl) && endpoint.Contains("{{BASE_URL}}", StringComparison.OrdinalIgnoreCase))
        {
            var cleanBaseUrl = baseUrl.TrimEnd('/');
            endpoint = endpoint.Replace("{{BASE_URL}}", cleanBaseUrl, StringComparison.OrdinalIgnoreCase);
            _log.LogDebug("Replaced {{BASE_URL}} placeholder in endpoint: {OriginalEndpoint} -> {NewEndpoint}", 
                originalEndpoint, endpoint);
        }
        return endpoint;
    }

    private HttpRequestMessage BuildHttpRequest(SimpleTest test, string endpoint)
    {
        var request = new HttpRequestMessage(new HttpMethod(test.Method), endpoint);

        // Headers
        if (test.Headers is { Count: > 0 })
        {
            foreach (var kv in test.Headers)
            {
                if (kv.Key.Equals(RunConstants.Http.ContentTypeHeader, StringComparison.OrdinalIgnoreCase))
                    request.Content ??= new StringContent(test.Body ?? string.Empty, Encoding.UTF8, kv.Value);
                else
                    request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
            }
        }

        // Accept JSON if hinted
        if (test.AcceptJson == true && !request.Headers.Accept.Any())
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(RunConstants.Http.JsonContentType));

        // Body
        if (!string.IsNullOrEmpty(test.Body) && request.Content is null)
            request.Content = new StringContent(test.Body, Encoding.UTF8,
                test.ContentType ?? RunConstants.Http.JsonContentType);

        return request;
    }

    private async Task SaveRequestArtifact(HttpRequestMessage request, HttpClient client, string name, SimpleTest test, string artPrefix)
    {
        var requestData = new
        {
            name,
            url = client.BaseAddress is null ? request.RequestUri?.ToString() ?? "" : 
                  new Uri(client.BaseAddress, request.RequestUri ?? new Uri("", UriKind.Relative)).ToString(),
            method = test.Method,
            headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
            contentHeaders = request.Content?.Headers?.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
            body = test.Body
        };

        await _fs.UploadAsync(
            RunConstants.Storage.ArtifactsContainer,
            $"{artPrefix}{RunConstants.Storage.RequestFileSuffix}",
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestData, Json))),
            RunConstants.Http.JsonContentType);
    }

    private async Task<TestResult> ProcessResponse(HttpResponseMessage resp, SimpleTest test, string name, DateTimeOffset startedAt)
    {
        var bodyText = await resp.Content.ReadAsStringAsync();
        var contentType = resp.Content.Headers.ContentType?.ToString();
        var statusOk = MatchesExpected(resp.StatusCode, test.ExpectedStatus ?? RunConstants.Http.DefaultExpectedStatus);
        var jsonOk = !(test.AcceptJson == true) || (contentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true);
        var ok = statusOk && jsonOk;

        return new TestResult(
            name, ok, (int)resp.StatusCode, contentType,
            resp.Headers.Concat(resp.Content.Headers)
                .ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
            bodyText, (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);
    }

    private async Task SaveResponseArtifact(TestResult result, string artPrefix)
    {
        await _fs.UploadAsync(
            RunConstants.Storage.ArtifactsContainer,
            $"{artPrefix}{RunConstants.Storage.ResponseFileSuffix}",
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result, Json))),
            RunConstants.Http.JsonContentType);
    }

    private async Task PersistResults(List<TestResult> results, Run run)
    {
        var pass = results.Count(r => r.Ok);
        var fail = results.Count - pass;

        await SaveSummary(results, run, pass, fail);
        await UpdateRunStatus(run, fail);
    }

    private async Task SaveSummary(List<TestResult> results, Run run, int pass, int fail)
    {
        var summaryData = new
        {
            runId = run.Id,
            startedAt = run.StartedAt,
            completedAt = DateTimeOffset.UtcNow,
            total = results.Count,
            passed = pass,
            failed = fail,
            results
        };

        var summaryPath = string.Format(RunConstants.Paths.SummaryPathTemplate, run.Id);
        await _fs.UploadAsync(
            RunConstants.Storage.ArtifactsContainer,
            summaryPath,
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(summaryData, Json))),
            RunConstants.Http.JsonContentType);
    }

    private async Task UpdateRunStatus(Run run, int failCount)
    {
        if (failCount == 0)
        {
            run.Complete(true);
        }
        else
        {
            run.Complete(false, string.Format(RunConstants.Messages.TestsFailedMessage, failCount));
        }

        await _runs.UpdateAsync(run);
    }

    private async Task HandleRunFailure(Run run, Exception ex, Guid runId)
    {
        _log.LogError(ex, "Run {RunId} failed.", runId);
        run.Complete(false, ex.Message);
        await _runs.UpdateAsync(run);
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