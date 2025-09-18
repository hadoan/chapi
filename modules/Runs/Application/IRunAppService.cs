using Runs.Application.Services;

namespace Runs.Application;

// Thin compatibility alias to the newer IRunsAppService to avoid migration churn.
public interface IRunAppService : IRunsAppService
{
}
