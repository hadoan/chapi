using System;
using System.Threading;
using System.Threading.Tasks;
using Contacts.Application.Dtos;
using Contacts.Application.Requests;
using Contacts.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Contacts.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class ContactController : ControllerBase
    {
        private readonly IContactService _service;
        private readonly IContactReadService _readService;

        public ContactController(IContactService service, IContactReadService readService)
        {
            _service = service;
            _readService = readService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateContactRequest request, CancellationToken ct)
        {
            var dto = await _service.CreateAsync(request, ct).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var dto = await _readService.GetByIdAsync(id, ct).ConfigureAwait(false);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var items = await _readService.ListAsync(page, pageSize, ct).ConfigureAwait(false);
            return Ok(items);
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContactRequest request, CancellationToken ct)
        {
            if (id != request.Id) return BadRequest("Id mismatch");

            var updated = await _service.UpdateAsync(request, ct).ConfigureAwait(false);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var deleted = await _service.DeleteAsync(id, ct).ConfigureAwait(false);
            return deleted ? NoContent() : NotFound();
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize]
        public async Task<IActionResult> SetStatus(Guid id, [FromQuery] string status, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(status)) return BadRequest("status is required");

            var ok = await _service.SetStatusAsync(id, status, ct).ConfigureAwait(false);
            return ok ? NoContent() : NotFound();
        }
    }
}
