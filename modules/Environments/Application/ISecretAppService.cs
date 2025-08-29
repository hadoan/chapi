using Environments.Application.Dtos;

namespace Environments.Application;

public interface ISecretAppService
{
    Task<EnvironmentDto> SetSecretAsync(Guid environmentId, SetSecretRequest request, CancellationToken ct = default);
    Task<EnvironmentDto> RotateSecretAsync(Guid environmentId, RotateSecretRequest request, CancellationToken ct = default);
}
