using HinataProject.Domain;

namespace HinataProject.Domain.Dto;

public class UserDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Info { get; set; } = string.Empty;

    public Rights Rights { get; set; }

    public List<UserRoleDto> Roles { get; set; } = new();
}

public class UserRoleDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
