using Suites.Application.Dtos;
using Suites.Domain;
using ShipMvp.Core.Attributes;

namespace Suites.Application;

[AutoController(Route = "api/suites")] 
public class SuiteAppService : ISuiteAppService
{
    private readonly ISuiteRepository _repository;
    public SuiteAppService(ISuiteRepository repository) => _repository = repository;

    public async Task<SuiteDto> CreateAsync(CreateSuiteRequest request, CancellationToken cancellationToken = default)
    {
        var suite = Suite.Create(request.Name, request.Description);
        suite.Update(request.Name, request.Description,
            request.Files.Select(f => (f.Path, f.Kind)),
            request.TestCases.Select(tc => (tc.Name, tc.Command)));
        var created = await _repository.AddAsync(suite, cancellationToken);
        return Map(created);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await _repository.DeleteAsync(id, cancellationToken);

    public async Task<SuiteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _repository.GetByIdAsync(id, cancellationToken);
        return e == null ? null : Map(e);
    }

    public async Task<IEnumerable<SuiteDto>> GetListAsync(GetSuitesQuery query, CancellationToken cancellationToken = default)
    {
        SuiteStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<SuiteStatus>(query.Status, true, out var s)) status = s;
        var (items, _) = await _repository.GetPagedAsync(query.Page, query.PageSize, status, query.Search, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<int> GetCountAsync(GetSuitesQuery query, CancellationToken cancellationToken = default)
    {
        SuiteStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<SuiteStatus>(query.Status, true, out var s)) status = s;
        var (_, total) = await _repository.GetPagedAsync(query.Page, query.PageSize, status, query.Search, cancellationToken);
        return total;
    }

    public async Task<SuiteDto> UpdateAsync(Guid id, UpdateSuiteRequest request, CancellationToken cancellationToken = default)
    {
        var suite = await _repository.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Suite not found");
        suite.Update(request.Name, request.Description,
            request.Files.Select(f => (f.Path, f.Kind)),
            request.TestCases.Select(tc => (tc.Name, tc.Command)));
        var updated = await _repository.UpdateAsync(suite, cancellationToken);
        return Map(updated);
    }

    public async Task<SuiteDto> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
        => await Transition(id, s => s.Activate(), cancellationToken);

    public async Task<SuiteDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
        => await Transition(id, s => s.Archive(), cancellationToken);

    private async Task<SuiteDto> Transition(Guid id, Action<Suite> action, CancellationToken ct)
    {
        var suite = await _repository.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Suite not found");
        action(suite);
        var updated = await _repository.UpdateAsync(suite, ct);
        return Map(updated);
    }

    private static SuiteDto Map(Suite s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        Status = s.Status.ToString(),
        CreatedAt = s.CreatedAt,
        Files = s.Files.Select(f => new SuiteFileDto(f.Id, f.Path, f.Kind)).ToList(),
        TestCases = s.TestCases.Select(tc => new TestCaseDto(tc.Id, tc.Name, tc.Command)).ToList()
    };
}
