using HinataProject.Domain.Core;
using NodaTime;

namespace HinataProject.Persistence.Entities;

public class NodeAssigneeEntity : IId<Guid>, IHaveCreatedDate, IHaveLastModifiedDate
{
    public Guid Id { get; set; }

    public Instant CreatedAt { get; set; }

    public Instant LastModifiedAt { get; set; }

    // Foreign keys
    public Guid NodeId { get; set; }

    public Guid RoleId { get; set; }

    public Guid UserId { get; set; }

    // Navigation properties
    public NodeEntity Node { get; set; } = null!;

    public RoleEntity Role { get; set; } = null!;

    public UserEntity User { get; set; } = null!;
}
