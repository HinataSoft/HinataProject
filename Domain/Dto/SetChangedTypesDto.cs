namespace HinataProject.Domain.Dto;

public class SetChangedTypesDto
{
    public List<TypeDto>? Added { get; set; }
    public List<Guid>? Removed { get; set; }
}

public class TypeDto
{
    public Guid? TypeId { get; set; }
    public TypeKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public Guid? DefaultWorkflowId { get; set; }
}
