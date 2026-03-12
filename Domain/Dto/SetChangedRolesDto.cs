namespace HinataProject.Domain.Dto;

public class SetChangedRolesDto
{
    public List<RoleDto>? Added { get; set; }
    public List<Guid>? Removed { get; set; }
}

public class RoleDto
{
    public Guid? RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Guid> States { get; set; } = new();
}
