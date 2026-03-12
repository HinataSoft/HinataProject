namespace HinataProject.Domain.Dto;

public class SetChangedWorkflowsDto
{
    public List<WorkflowDto>? Added { get; set; }
    public List<Guid>? Removed { get; set; }
}

public class WorkflowDto
{
    public string? WorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? DefaultStateId { get; set; }
    public List<StateDto> States { get; set; } = new();
}

public class StateDto
{
    public string? StateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsFinalSuccess { get; set; }
    public bool IsFinalFailure { get; set; }
}
