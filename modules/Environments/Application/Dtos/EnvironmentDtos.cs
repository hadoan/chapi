namespace Environments.Application.Dtos;

public record EnvironmentHeaderDto(string Key, string Value);
public record SecretRefDto(string Provider, string KeyPath, DateTime? LastRotatedAt, string MaskedPreview);

public record EnvironmentDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutMs { get; init; }
    public bool FollowRedirects { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<EnvironmentHeaderDto> Headers { get; init; } = Array.Empty<EnvironmentHeaderDto>();
    public IReadOnlyList<SecretRefDto> Secrets { get; init; } = Array.Empty<SecretRefDto>();
}

public record CreateEnvironmentRequest
{
    public string Name { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutMs { get; init; } = 30000;
    public bool FollowRedirects { get; init; } = true;
    public List<EnvironmentHeaderDto> Headers { get; init; } = new();
}

public record UpdateEnvironmentRequest
{
    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutMs { get; init; } = 30000;
    public bool FollowRedirects { get; init; } = true;
    public List<EnvironmentHeaderDto> Headers { get; init; } = new();
}

public record GetEnvironmentsQuery
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record SetSecretRequest
{
    public string Provider { get; init; } = string.Empty;
    public string KeyPath { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty; // Will not be returned raw
}

public record RotateSecretRequest
{
    public string Provider { get; init; } = string.Empty;
    public string KeyPath { get; init; } = string.Empty;
}
