using System;
using System.Threading.Tasks;

namespace Chapi.AI.Services
{
    public interface IEndpointContextService
    {
        /// <summary>
        /// Build a compact, deterministic endpoint context block for the given project.
        /// </summary>
        /// <param name="projectId">Project id to fetch endpoints for.</param>
        /// <param name="maxItems">Maximum endpoints to include.</param>
        Task<string> BuildContextAsync(Guid projectId, int maxItems = 25);
    }
}
