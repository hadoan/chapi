using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using AuthProfiles.Application.Services;
using AuthProfiles.Application.Requests;
using AuthProfiles.Application.Dtos;

namespace AuthProfiles.Controllers
{
    [ApiController]
    [Route("api/authprofiles")]
    public class AuthProfilesController : ControllerBase
    {
        private readonly IAuthProfileService _service;
        private readonly IAuthProfileReadService _read;
        private readonly IAuthDetectionService _detection;

        private readonly IAuthProfileTester _authProfileTester;

        public AuthProfilesController(IAuthProfileService service, IAuthProfileReadService read, IAuthDetectionService detection, IAuthProfileTester authProfileTester)
        {
            _service = service;
            _read = read;
            _detection = detection;
            _authProfileTester = authProfileTester;
        }

        [HttpPost]
        public async Task<AuthProfileDto> Create([FromBody] CreateAuthProfileRequest r, CancellationToken ct)
        {
            var dto = await _service.CreateAsync(r, ct).ConfigureAwait(false);
            Response.StatusCode = StatusCodes.Status201Created;
            var location = Url.Action(nameof(GetById), new { id = dto.Id });
            if (!string.IsNullOrEmpty(location)) Response.Headers["Location"] = location;
            return dto;
        }

        [HttpGet("{id:guid}")]
        public async Task<AuthProfileDto?> GetById(Guid id, CancellationToken ct)
        {
            var dto = await _read.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (dto == null) Response.StatusCode = StatusCodes.Status404NotFound;
            return dto;
        }

        // Get first valid (enabled) auth profile for a project and environment
        [HttpGet("first")]
        public async Task<ActionResult<AuthProfileDto>> GetFirstForProjectEnvironment([FromQuery] Guid projectId, [FromQuery] string environmentKey, CancellationToken ct)
        {
            if (projectId == Guid.Empty || string.IsNullOrWhiteSpace(environmentKey)) return BadRequest("projectId and environmentKey are required");
            var dto = await _read.GetFirstForProjectEnvironmentAsync(projectId, environmentKey, ct).ConfigureAwait(false);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpGet]
        public async Task<IEnumerable<AuthProfileDto>> List([FromQuery] GetAuthProfilesQuery q, CancellationToken ct)
        {
            var (items, total) = await _read.ListAsync(q, ct).ConfigureAwait(false);
            Response.Headers["X-Total-Count"] = total.ToString();
            return items;
        }

        [HttpPut("{id:guid}")]
        public async Task<AuthProfileDto> Update(Guid id, [FromBody] UpdateAuthProfileRequest r, CancellationToken ct)
        {
            var dto = await _service.UpdateAsync(id, r, ct).ConfigureAwait(false);
            return dto;
        }

        [HttpDelete("{id:guid}")]
        public async Task Delete(Guid id, CancellationToken ct)
        {
            await _service.DeleteAsync(id, ct).ConfigureAwait(false);
            Response.StatusCode = StatusCodes.Status204NoContent;
        }

        [HttpPost("{id:guid}/enable")]
        public async Task Enable(Guid id, CancellationToken ct)
        {
            await _service.EnableAsync(id, ct).ConfigureAwait(false);
            Response.StatusCode = StatusCodes.Status204NoContent;
        }

        [HttpPost("{id:guid}/disable")]
        public async Task Disable(Guid id, CancellationToken ct)
        {
            await _service.DisableAsync(id, ct).ConfigureAwait(false);
            Response.StatusCode = StatusCodes.Status204NoContent;
        }

        [HttpPost("detect/legacy")]
        public async Task<IReadOnlyList<AuthDetectionCandidateDto>> DetectLegacy([FromBody] DetectRequest r, CancellationToken ct)
        {
            var candidates = await _detection.DetectAsync(r.ProjectId, r.ServiceId, ct).ConfigureAwait(false);
            return candidates;
        }

        [HttpPost("test")]
        public async Task<TestAuthResult> Test([FromBody] TestRequest r, CancellationToken ct)
        {
            // resolve and run test via service - implementation added in application layer
            if (_authProfileTester == null) return new TestAuthResult(false, "error", "Tester not available", null, null, null, null, null, null);

            var dto = new Application.Dtos.TestAuthRequest(
                r.AuthProfileId.GetValueOrDefault(),
                r.ProfileInline,
                r.EnvId,
                r.OverrideSecretValues
            );

            var res = await _authProfileTester.TestAsync(dto, ct).ConfigureAwait(false);
            return res;
        }

        public class TestRequest
        {
            public Guid? AuthProfileId { get; set; }
            public AuthProfiles.Application.Dtos.AuthProfileDto? ProfileInline { get; set; }
            public Guid? EnvId { get; set; }
            public IDictionary<string, string>? OverrideSecretValues { get; set; }
        }

        public class DetectRequest
        {
            public Guid ProjectId { get; set; }
            public Guid ServiceId { get; set; }
        }
    }
}
