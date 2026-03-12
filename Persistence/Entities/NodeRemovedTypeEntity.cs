using HinataProject.Domain.Core;
using NodaTime;

namespace HinataProject.Persistence.Entities;

public class NodeRemovedTypeEntity : IId<Guid>, IHaveCreatedDate
{
    public Guid Id { get; set; }

    public Instant CreatedAt { get; set; }

    // Foreign keys
    public Guid NodeId { get; set; }

    public Guid TypeId { get; set; }

    // Navigation properties
    public NodeEntity Node { get; set; } = null!;

    public TypeEntity Type { get; set; } = null!;
}
