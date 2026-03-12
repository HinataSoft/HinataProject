using HinataProject.Domain;

namespace HinataProject.Domain.Dto;

public class CreateUserDto
{
    public string Name { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string? Info { get; set; }

    public Rights Rights { get; set; } = Rights.Passive;
}
