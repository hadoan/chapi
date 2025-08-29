using Environments.Application.Dtos;
using Environments.Domain;
using ShipMvp.Core.Attributes;
using ShipMvp.Core.Abstractions;

namespace Environments.Application;

[AutoController(Route = "api/environments/{environmentId:guid}/secrets")]
public class SecretAppService : ISecretAppService
{
    private readonly IEnvironmentRepository _repository;
    private readonly ISecretStore _secretStore;
    private readonly IEventBus _eventBus;

    public SecretAppService(IEnvironmentRepository repository, ISecretStore secretStore, IEventBus eventBus)
    {
        _repository = repository;
        _secretStore = secretStore;
        _eventBus = eventBus;
    }

    public async Task<EnvironmentDto> SetSecretAsync(Guid environmentId, SetSecretRequest request, CancellationToken ct = default)
    {
        var env = await _repository.GetByIdAsync(environmentId, ct) ?? throw new InvalidOperationException("Environment not found");
        await _secretStore.SetAsync(request.Provider, request.KeyPath, request.Value, ct);
        var existing = env.Secrets.FirstOrDefault(s => s.Provider == request.Provider && s.KeyPath == request.KeyPath);
        if (existing == null)
            env.Secrets.Add(new SecretVaultRef(Guid.NewGuid(), request.Provider, request.KeyPath));
        var updated = await _repository.UpdateAsync(env, ct);
        await _eventBus.PublishAsync(new SecretsUpdatedEvent(environmentId, request.Provider, request.KeyPath, false), ct);
        return EnvironmentAppServiceReflector.Map(updated);
    }

    public async Task<EnvironmentDto> RotateSecretAsync(Guid environmentId, RotateSecretRequest request, CancellationToken ct = default)
    {
        var env = await _repository.GetByIdAsync(environmentId, ct) ?? throw new InvalidOperationException("Environment not found");
        await _secretStore.RotateAsync(request.Provider, request.KeyPath, ct);
        var existing = env.Secrets.FirstOrDefault(s => s.Provider == request.Provider && s.KeyPath == request.KeyPath);
        if (existing != null) existing.Rotated();
        var updated = await _repository.UpdateAsync(env, ct);
        await _eventBus.PublishAsync(new SecretsUpdatedEvent(environmentId, request.Provider, request.KeyPath, true), ct);
        return EnvironmentAppServiceReflector.Map(updated);
    }
}

// Helper to reuse mapping of EnvironmentAppService without internal duplication
internal static class EnvironmentAppServiceReflector
{
    public static Environments.Application.Dtos.EnvironmentDto Map(Environments.Domain.Environment e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        BaseUrl = e.BaseUrl,
        TimeoutMs = e.TimeoutMs,
        FollowRedirects = e.FollowRedirects,
        CreatedAt = e.CreatedAt,
        Headers = e.Headers.Select(h => new Environments.Application.Dtos.EnvironmentHeaderDto(h.Key, h.Value)).ToList(),
        Secrets = e.Secrets.Select(s => new Environments.Application.Dtos.SecretRefDto(s.Provider, s.KeyPath, s.LastRotatedAt, Mask(s.KeyPath))).ToList()
    };

    private static string Mask(string keyPath)
        => string.IsNullOrEmpty(keyPath) ? string.Empty : (keyPath.Length <= 4 ? new string('*', keyPath.Length) : new string('*', keyPath.Length - 4) + keyPath[^4..]);
}

public record SecretsUpdatedEvent(Guid EnvironmentId, string Provider, string KeyPath, bool Rotated);
