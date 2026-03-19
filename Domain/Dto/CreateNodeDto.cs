namespace HinataProject.Domain.Dto;

public class CreateNodeDto
{
    public Guid ParentId { get; set; }
    public Guid TypeId { get; set; }
    public Guid? WorkflowId { get; set; }
    public string? Manifest { get; set; }
    public string? Caption { get; set; }
    public string? Description { get; set; }
    public string? Guardrails { get; set; }
}
