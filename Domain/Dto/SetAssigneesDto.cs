namespace HinataProject.Domain.Dto;

public class SetAssigneesDto
{
    public Dictionary<Guid, Guid> Assignees { get; set; } = new();
}
