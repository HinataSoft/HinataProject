using HinataProject.Domain.Core;
using NodaTime;

namespace HinataProject.Persistence.Entities;

public class NodeRemovedRoleEntity : IId<Guid>, IHaveCreatedDate
{
    public Guid Id { get; set; }

    public Instant CreatedAt { get; set; }

    // Foreign keys
    public Guid NodeId { get; set; }

    public Guid RoleId { get; set; }

    // Navigation properties
    public NodeEntity Node { get; set; } = null!;

    public RoleEntity Role { get; set; } = null!;
}
