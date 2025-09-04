using ShipMvp.Core.Entities;

namespace RunPack.Domain;

public class RunPackInput : Entity<Guid>
{
    public Guid RunPackId { get; private set; }
    public string FileRolesJson { get; private set; } = string.Empty;
    public string RoleContextsJson { get; private set; } = string.Empty;
    public string EndpointsContext { get; private set; } = string.Empty;
    public string AllowedOps { get; private set; } = string.Empty;
    public string Environment { get; private set; } = string.Empty;
    public Guid? ProjectId { get; private set; }
    public Guid? SuiteId { get; private set; }
    public string? AiModel { get; private set; }
    public double? Temperature { get; private set; }
    public int? MaxTokens { get; private set; }
    public int? ContextSize { get; private set; }
    public string? StopSequences { get; private set; }

    private RunPackInput() : base(Guid.Empty) { }

    private RunPackInput(Guid id, Guid runPackId, string fileRolesJson, string roleContextsJson, 
        string endpointsContext, string allowedOps, string environment) : base(id)
    {
        RunPackId = runPackId;
        FileRolesJson = fileRolesJson;
        RoleContextsJson = roleContextsJson;
        EndpointsContext = endpointsContext;
        AllowedOps = allowedOps;
        Environment = environment;
        CreatedAt = DateTime.UtcNow;
    }

    public static RunPackInput Create(Guid runPackId, string fileRolesJson, string roleContextsJson, 
        string endpointsContext, string allowedOps, string environment)
        => new(Guid.NewGuid(), runPackId, fileRolesJson, roleContextsJson, endpointsContext, allowedOps, environment);

    public void SetProjectId(Guid projectId) => ProjectId = projectId;
    public void SetSuiteId(Guid suiteId) => SuiteId = suiteId;
    public void SetAiModel(string aiModel) => AiModel = aiModel;
    public void SetTemperature(double temperature) => Temperature = temperature;
    public void SetMaxTokens(int maxTokens) => MaxTokens = maxTokens;
    public void SetContextSize(int contextSize) => ContextSize = contextSize;
    public void SetStopSequences(string stopSequences) => StopSequences = stopSequences;
}
