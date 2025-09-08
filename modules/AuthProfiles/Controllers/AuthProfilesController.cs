using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
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

        public AuthProfilesController(IAuthProfileService service, IAuthProfileReadService read, IAuthDetectionService detection)
        {
            _service = service;
            _read = read;
            _detection = detection;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAuthProfileRequest r, CancellationToken ct)
        {
            var dto = await _service.CreateAsync(r, ct).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var dto = await _read.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] GetAuthProfilesQuery q, CancellationToken ct)
        {
            var (items, total) = await _read.ListAsync(q, ct).ConfigureAwait(false);
            Response.Headers["X-Total-Count"] = total.ToString();
            return Ok(items);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAuthProfileRequest r, CancellationToken ct)
        {
            var dto = await _service.UpdateAsync(id, r, ct).ConfigureAwait(false);
            return Ok(dto);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _service.DeleteAsync(id, ct).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id:guid}/enable")]
        public async Task<IActionResult> Enable(Guid id, CancellationToken ct)
        {
            await _service.EnableAsync(id, ct).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id:guid}/disable")]
        public async Task<IActionResult> Disable(Guid id, CancellationToken ct)
        {
            await _service.DisableAsync(id, ct).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("detect")]
        public async Task<IActionResult> Detect([FromBody] DetectRequest r, CancellationToken ct)
        {
            var candidates = await _detection.DetectAsync(r.ProjectId, r.ServiceId, ct).ConfigureAwait(false);
            return Ok(candidates);
        }

        public class DetectRequest
        {
            public Guid ProjectId { get; set; }
            public Guid ServiceId { get; set; }
        }
    }
}
