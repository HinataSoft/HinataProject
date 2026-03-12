using HinataProject.Domain.Core;
using NodaTime;

namespace HinataProject.Persistence.Entities;

public class StateEntity : IId<Guid>, IHaveCreatedDate, IHaveLastModifiedDate
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsFinalSuccess { get; set; }

    public bool IsFinalFailure { get; set; }

    public Instant CreatedAt { get; set; }

    public Instant LastModifiedAt { get; set; }

    // Navigation properties
    public Guid WorkflowId { get; set; }

    public WorkflowEntity Workflow { get; set; } = null!;

    public ICollection<RoleEntity> Roles { get; set; } = new List<RoleEntity>();
}
