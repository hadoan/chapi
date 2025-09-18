using Runs.Domain;

namespace Runs.Application.Ports;

public interface IRunScheduler
{
    Task EnqueueAsync(Run run, CancellationToken ct = default);
}