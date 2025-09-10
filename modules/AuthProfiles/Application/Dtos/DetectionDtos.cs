using System.Text.Json.Serialization;

namespace AuthProfiles.Application.Dtos;

public sealed record DetectTokenRequest(
    Guid? ProjectId,
    Guid? ServiceId,
    string? BaseUrl,
    string? OpenApiJson,
    string? PostmanJson
);

public sealed record DetectionCandidateDto(
    string Type,                 // "oauth2_client_credentials" | "api_key_header" | "bearer_static" | "session_cookie"
    string Endpoint,             // "/connect/token"
    string? TokenUrl,            // absolute if we can build it
    InjectionPreview Injection,  // how we’d inject if chosen
    string Source,               // "openapi" | "postman" | "heuristic"
    double Confidence
);

public sealed record InjectionPreview(string Mode, string Name, string Format);

public sealed record DetectionResponse(
    IReadOnlyList<DetectionCandidateDto> Candidates,
    SimpleDetection? Best
);

public sealed record SimpleDetection(string Endpoint, string Source, double Confidence);
