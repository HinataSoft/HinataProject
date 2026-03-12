using HinataProject.Domain;
using HinataProject.Domain.Core;
using NodaTime;

namespace HinataProject.Persistence.Entities;

public class TypeEntity : IId<Guid>, IHaveCreatedDate, IHaveLastModifiedDate
{
    public Guid Id { get; set; }

    public TypeKind Kind { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public Guid? DefaultWorkflowId { get; set; }

    public Instant CreatedAt { get; set; }

    public Instant LastModifiedAt { get; set; }

    // Navigation properties
    public ICollection<NodeEntity> Nodes { get; set; } = new List<NodeEntity>();

    public WorkflowEntity? DefaultWorkflow { get; set; }
}
