namespace Suites.Application.Dtos;

public record SuiteFileDto(Guid Id, string Path, string Kind);
public record TestCaseDto(Guid Id, string Name, string Command);

public class SuiteDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<SuiteFileDto> Files { get; set; } = new();
    public List<TestCaseDto> TestCases { get; set; } = new();
}

public record CreateSuiteRequest(string Name, string? Description, List<CreateSuiteFileRequest> Files, List<CreateTestCaseRequest> TestCases);
public record CreateSuiteFileRequest(string Path, string Kind);
public record CreateTestCaseRequest(string Name, string Command);
public record UpdateSuiteRequest(string Name, string? Description, List<CreateSuiteFileRequest> Files, List<CreateTestCaseRequest> TestCases);
public record GetSuitesQuery(int Page = 1, int PageSize = 20, string? Status = null, string? Search = null);
