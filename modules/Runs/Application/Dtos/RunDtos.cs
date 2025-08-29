namespace Runs.Application.Dtos;

public record RunStepDto(Guid Id, string Name, string? Log);
public class RunDto
{
    public Guid Id { get; set; }
    public Guid SuiteId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public List<RunStepDto> Steps { get; set; } = new();
}

public record QueueRunRequest(Guid SuiteId);
public record AddRunStepRequest(string Name, string? Log);
public record GetRunsQuery(int Page = 1, int PageSize = 20, Guid? SuiteId = null, string? Status = null);
