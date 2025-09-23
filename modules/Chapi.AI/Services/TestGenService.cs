using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Chapi.AI.Dto;
using ShipMvp.Core.Abstractions;
using Chapi.EndpointCatalog.Application;
using AuthProfiles.Application.Dtos;
using Google.Apis.Util;

namespace Chapi.AI.Services
{
    public class TestGenService : ITestGenService
    {
        private readonly ILogger<TestGenService> _logger;
        private readonly ITestGenCardGenerator _cardGenerator;
        private readonly ITestGenFileGenerator _fileGenerator;
        private readonly ITestGenDatabasePersistenceService _databaseService;
        private readonly IGuidGenerator _guidGenerator;

        public TestGenService(
            ILogger<TestGenService> logger,
            ITestGenCardGenerator cardGenerator,
            ITestGenFileGenerator fileGenerator,
            IGuidGenerator guidGenerator,
            ITestGenDatabasePersistenceService databaseService)
        {
            _logger = logger;
            _guidGenerator = guidGenerator;
            _cardGenerator = cardGenerator;
            _fileGenerator = fileGenerator; // keep consistent with field
            _databaseService = databaseService;
        }

        public async Task<TestGenResponse> GenerateTestsAsync(TestGenInput input, bool generateJson = true)
        {
            _logger.LogInformation("Generating tests for endpoint {Method} {Path}",
                input.SelectedEndpoint.Method, input.SelectedEndpoint.Path);

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var card = _cardGenerator.GenerateCard(input);

            List<TestGenFile>? files = null;
            if (input.Mode == "FILES")
            {
                files = _fileGenerator.GenerateFiles(input.SelectedEndpoint, input.AuthProfile, input.Options, generateJson);
            }

            // Create database operations using the database service (now saves to database)
            var dbOps = await _databaseService.SaveDatabaseOperationsAsync(input, card, timestamp, files);

            return new TestGenResponse
            {
                Role = "Chapi",
                Card = card,
                Files = files,
                DbOps = dbOps
            };
        }

        public async Task<TestGenResponse> GenerateEnpointTestsAsync(EndpointDto endpointDto, AuthProfileDto authProfile)
        {
            if (endpointDto == null) throw new ArgumentNullException(nameof(endpointDto));

            _logger.LogInformation("Generating tests for endpoint {Method} {Path}", endpointDto.Method, endpointDto.Path);

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Convert EndpointDto to SelectedEndpoint used by generators
            var selectedEndpoint = new SelectedEndpoint
            {
                Id = endpointDto.Id,
                Method = endpointDto.Method ?? "GET",
                Path = endpointDto.Path ?? string.Empty,
                Summary = endpointDto.Summary,
                RequiresAuth = (endpointDto.Security?.Count ?? 0) > 0,
                SuccessCode = DetermineSuccessCode(endpointDto),
                RequestSchemaHint = endpointDto.Request != null ? "object" : "none"
            };

            // Build a minimal TestGenInput for generation
            var input = new TestGenInput
            {
                
                Mode = "FILES",
                SelectedEndpoint = selectedEndpoint,
                AuthProfile = MapAuthProfileDto(authProfile),
                Options = new TestGenOptions(),
                Project = new ProjectInfo() { Id= endpointDto.ProjectId.ToString()},
                Chat = new ChatInfo(),
                UserQuery = string.Empty
            };

            var card = _cardGenerator.GenerateCard(input);

            List<TestGenFile>? files = null;
            // Generate files for this endpoint
            files = _fileGenerator.GenerateFiles(input.SelectedEndpoint, input.AuthProfile, input.Options, false);

            // Persist database operations
            var dbOps = await _databaseService.SaveDatabaseOperationsAsync(input, card, timestamp, files);

            return new TestGenResponse
            {
                Role = "Chapi",
                Card = card,
                Files = files,
                DbOps = dbOps
            };
        }

        private Chapi.AI.Dto.AuthProfile MapAuthProfileDto(AuthProfileDto dto)
        {
            var cfg = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            static string? ParamToString(object? val)
            {
                if (val == null) return null;
                if (val is JsonElement je)
                {
                    return je.ValueKind switch
                    {
                        JsonValueKind.String => je.GetString(),
                        JsonValueKind.Number => je.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => null,
                        _ => je.GetRawText()
                    };
                }

                return val.ToString();
            }

            if (dto?.Params != null)
            {
                foreach (var kv in dto.Params)
                {
                    try
                    {
                        var s = ParamToString(kv.Value);
                        if (!string.IsNullOrEmpty(s)) cfg[kv.Key] = s;
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            if (dto?.SecretRefs != null)
            {
                foreach (var kv in dto.SecretRefs)
                {
                    if (kv.Value != null)
                        cfg[kv.Key] = kv.Value;
                }
            }

            var profile = new Chapi.AI.Dto.AuthProfile
            {
                Id = dto?.Id?.ToString() ?? string.Empty,
                Name = dto?.Type.ToString() ?? string.Empty,
                Type = dto?.Type switch
                {
                    AuthProfiles.Domain.AuthType.OAuth2ClientCredentials => "OIDC_CLIENT_CREDENTIALS",
                    AuthProfiles.Domain.AuthType.OAuth2Password => "OIDC_PASSWORD",
                    AuthProfiles.Domain.AuthType.Basic => "BASIC",
                    AuthProfiles.Domain.AuthType.BearerStatic => "BEARER",
                    AuthProfiles.Domain.AuthType.ApiKeyHeader => "API_KEY",
                    AuthProfiles.Domain.AuthType.CustomLogin => "CUSTOM_SCRIPT",
                    _ => "NONE"
                },
                Config = cfg
            };

            if (cfg.TryGetValue("token_url", out var turl) && !profile.Config.ContainsKey("TOKEN_URL")) profile.Config["TOKEN_URL"] = turl;
            if (cfg.TryGetValue("client_id", out var cid) && !profile.Config.ContainsKey("CLIENT_ID")) profile.Config["CLIENT_ID"] = cid;
            if (cfg.TryGetValue("client_secret", out var csecret) && !profile.Config.ContainsKey("CLIENT_SECRET")) profile.Config["CLIENT_SECRET"] = csecret;

            return profile;
        }

        private static int DetermineSuccessCode(EndpointDto dto)
        {
            try
            {
                if (dto?.Responses != null && dto.Responses.Count > 0)
                {
                    // Prefer first 2xx status code
                    var twoXx = dto.Responses.Keys.FirstOrDefault(k => k.StartsWith("2"));
                    if (!string.IsNullOrEmpty(twoXx) && int.TryParse(twoXx, out var v)) return v;

                    // Fallback to 200 if present
                    if (dto.Responses.ContainsKey("200")) return 200;
                    if (dto.Responses.ContainsKey("201")) return 201;

                    // Otherwise pick the first numeric status code
                    var firstNumeric = dto.Responses.Keys.Select(k => { int.TryParse(k, out var n); return n; }).FirstOrDefault(n => n != 0);
                    if (firstNumeric != 0) return firstNumeric;
                }
            }
            catch
            {
                // ignore and fallthrough
            }

            return 200;
        }
    }
}
