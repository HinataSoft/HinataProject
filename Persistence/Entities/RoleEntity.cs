using HinataProject.Domain.Core;
using NodaTime;

namespace HinataProject.Persistence.Entities;

public class RoleEntity : IId<Guid>, IHaveCreatedDate, IHaveLastModifiedDate
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Instant CreatedAt { get; set; }

    public Instant LastModifiedAt { get; set; }

    // Navigation properties - via UserRoleEntity
    public ICollection<UserRoleEntity> UserRoles { get; set; } = new List<UserRoleEntity>();

    public ICollection<StateEntity> States { get; set; } = new List<StateEntity>();

    public ICollection<NodeAssigneeEntity> NodeAssignees { get; set; } = new List<NodeAssigneeEntity>();

    public ICollection<NodeAddedRoleEntity> AddedRoleNodes { get; set; } = new List<NodeAddedRoleEntity>();
}
