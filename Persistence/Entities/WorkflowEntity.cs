using HinataProject.Domain.Core;
using NodaTime;

namespace HinataProject.Persistence.Entities;

public class WorkflowEntity : IId<Guid>, IHaveCreatedDate, IHaveLastModifiedDate
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Instant CreatedAt { get; set; }

    public Instant LastModifiedAt { get; set; }

    // Navigation properties
    public Guid? DefaultStateId { get; set; }

    public StateEntity? DefaultState { get; set; }

    public ICollection<StateEntity> States { get; set; } = new List<StateEntity>();

    public ICollection<NodeEntity> Nodes { get; set; } = new List<NodeEntity>();
}
