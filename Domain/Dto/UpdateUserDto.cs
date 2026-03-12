using HinataProject.Domain;

namespace HinataProject.Domain.Dto;

public class UpdateUserDto
{
    public string? Name { get; set; }

    public string? Subject { get; set; }

    public string? Info { get; set; }

    public Rights? Rights { get; set; }
}
