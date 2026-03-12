using HinataProject.Domain;
using HinataProject.Domain.Core;
using NodaTime;

namespace HinataProject.Persistence.Entities;

public class UserEntity : IId<Guid>, IHaveCreatedDate, IHaveLastModifiedDate
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Info { get; set; } = string.Empty;

    public Rights Rights { get; set; }

    public Instant CreatedAt { get; set; }

    public Instant LastModifiedAt { get; set; }

    // Navigation properties - via UserRoleEntity
    public ICollection<UserRoleEntity> UserRoles { get; set; } = new List<UserRoleEntity>();

    public ICollection<CommentEntity> Comments { get; set; } = new List<CommentEntity>();

    public ICollection<NodeAssigneeEntity> NodeAssignees { get; set; } = new List<NodeAssigneeEntity>();

    public ICollection<AuditLogEntryEntity> AuditLogEntries { get; set; } = new List<AuditLogEntryEntity>();
}
