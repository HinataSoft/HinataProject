using HinataProject.Domain.Core;
using NodaTime;

namespace HinataProject.Persistence.Entities;

public class UserRoleEntity : IId<Guid>, IHaveCreatedDate, IHaveLastModifiedDate
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public Instant CreatedAt { get; set; }

    public Instant LastModifiedAt { get; set; }

    // Navigation properties
    public UserEntity? User { get; set; }

    public RoleEntity? Role { get; set; }
}
