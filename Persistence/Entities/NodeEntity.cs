using HinataProject.Domain.Core;
using NodaTime;

namespace HinataProject.Persistence.Entities;

public class NodeEntity : IId<Guid>, IHaveCreatedDate, IHaveLastModifiedDate
{
    public Guid Id { get; set; }

    // Topological properties
    public Guid? ParentId { get; set; }

    public int PublicId { get; set; }

    // Descriptive properties
    public string Manifest { get; set; } = string.Empty;

    public string Caption { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Guardrails { get; set; } = string.Empty;

    public Instant CreatedAt { get; set; }

    public Instant LastModifiedAt { get; set; }

    // Foreign keys
    public Guid? TypeId { get; set; }

    public Guid? WorkflowId { get; set; }

    public Guid? StateId { get; set; }

    // Navigation properties
    public NodeEntity? Parent { get; set; }

    public ICollection<NodeEntity> Children { get; set; } = new List<NodeEntity>();

    public TypeEntity? Type { get; set; }

    public WorkflowEntity? Workflow { get; set; }

    public StateEntity? State { get; set; }

    public ICollection<CommentEntity> Comments { get; set; } = new List<CommentEntity>();

    public ICollection<AuditLogEntryEntity> AuditLogEntries { get; set; } = new List<AuditLogEntryEntity>();

    public ICollection<NodeAssigneeEntity> Assignees { get; set; } = new List<NodeAssigneeEntity>();

    // Changed settings (for structural nodes)
    public ICollection<NodeAddedTypeEntity> AddedTypes { get; set; } = new List<NodeAddedTypeEntity>();

    public ICollection<NodeRemovedTypeEntity> RemovedTypes { get; set; } = new List<NodeRemovedTypeEntity>();

    public ICollection<NodeAddedWorkflowEntity> AddedWorkflows { get; set; } = new List<NodeAddedWorkflowEntity>();

    public ICollection<NodeRemovedWorkflowEntity> RemovedWorkflows { get; set; } = new List<NodeRemovedWorkflowEntity>();

    public ICollection<NodeAddedRoleEntity> AddedRoles { get; set; } = new List<NodeAddedRoleEntity>();

    public ICollection<NodeRemovedRoleEntity> RemovedRoles { get; set; } = new List<NodeRemovedRoleEntity>();
}
