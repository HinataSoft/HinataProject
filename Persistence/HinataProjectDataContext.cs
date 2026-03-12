using HinataProject.Domain.Core;
using HinataProject.Persistence.Entities;
using HinataProject.Persistence.Utils;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;

namespace HinataProject.Persistence;

public class HinataProjectDataContext : DbContext
{
    private readonly TimeProvider timeProvider;

    public HinataProjectDataContext(DbContextOptions<HinataProjectDataContext> options,
        TimeProvider timeProvider) : base(options)
    {
        this.timeProvider = timeProvider;
    }

    // DbSet properties
    public DbSet<TypeEntity> Types { get; set; } = null!;
    public DbSet<StateEntity> States { get; set; } = null!;
    public DbSet<WorkflowEntity> Workflows { get; set; } = null!;
    public DbSet<RoleEntity> Roles { get; set; } = null!;
    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<CommentEntity> Comments { get; set; } = null!;
    public DbSet<NodeAssigneeEntity> NodeAssignees { get; set; } = null!;
    public DbSet<NodeEntity> Nodes { get; set; } = null!;
    public DbSet<AuditLogEntryEntity> AuditLogEntries { get; set; } = null!;

    // Changed properties join tables
    public DbSet<NodeAddedTypeEntity> NodeAddedTypes { get; set; } = null!;
    public DbSet<NodeRemovedTypeEntity> NodeRemovedTypes { get; set; } = null!;
    public DbSet<NodeAddedWorkflowEntity> NodeAddedWorkflows { get; set; } = null!;
    public DbSet<NodeRemovedWorkflowEntity> NodeRemovedWorkflows { get; set; } = null!;
    public DbSet<NodeAddedRoleEntity> NodeAddedRoles { get; set; } = null!;
    public DbSet<NodeRemovedRoleEntity> NodeRemovedRoles { get; set; } = null!;
    public DbSet<UserRoleEntity> UserRoles { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var mutableEntityType in modelBuilder.Model.GetEntityTypes())
        {
            if (mutableEntityType.ClrType.IsAssignableTo(typeof(IId<Guid>)))
                mutableEntityType.GetProperty(nameof(IId<Guid>.Id))
                    .SetValueGeneratorFactory((_, _) => new GuidV7Generator());

            foreach (var enumProperty in mutableEntityType.GetProperties()
                         .Where(q => (Nullable.GetUnderlyingType(q.ClrType) ?? q.ClrType).IsEnum))
                modelBuilder.Entity(mutableEntityType.ClrType).Property(enumProperty.Name).HasConversion<string>();
        }

        // Configure Node self-referencing relationship
        modelBuilder.Entity<NodeEntity>()
            .HasOne(n => n.Parent)
            .WithMany(n => n.Children)
            .HasForeignKey(n => n.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Node -> Type relationship
        modelBuilder.Entity<NodeEntity>()
            .HasOne(n => n.Type)
            .WithMany(t => t.Nodes)
            .HasForeignKey(n => n.TypeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Node -> Workflow relationship
        modelBuilder.Entity<NodeEntity>()
            .HasOne(n => n.Workflow)
            .WithMany(w => w.Nodes)
            .HasForeignKey(n => n.WorkflowId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Node -> State relationship
        modelBuilder.Entity<NodeEntity>()
            .HasOne(n => n.State)
            .WithMany()
            .HasForeignKey(n => n.StateId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure State -> Workflow relationship
        modelBuilder.Entity<StateEntity>()
            .HasOne(s => s.Workflow)
            .WithMany(w => w.States)
            .HasForeignKey(s => s.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Workflow -> DefaultState relationship
        modelBuilder.Entity<WorkflowEntity>()
            .HasOne(w => w.DefaultState)
            .WithMany()
            .HasForeignKey(w => w.DefaultStateId)
            .OnDelete(DeleteBehavior.SetNull);

        

        // Configure Role -> States (many-to-many)
        modelBuilder.Entity<RoleEntity>()
            .HasMany(r => r.States)
            .WithMany(s => s.Roles)
            .UsingEntity(j => j.ToTable("role_states"));

        // Configure User -> Roles (via UserRoleEntity)
        modelBuilder.Entity<UserRoleEntity>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRoleEntity>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Comment -> Node relationship
        modelBuilder.Entity<CommentEntity>()
            .HasOne(c => c.Node)
            .WithMany(n => n.Comments)
            .HasForeignKey(c => c.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Comment -> User relationship
        modelBuilder.Entity<CommentEntity>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure NodeAssignee -> Node relationship
        modelBuilder.Entity<NodeAssigneeEntity>()
            .HasOne(na => na.Node)
            .WithMany(n => n.Assignees)
            .HasForeignKey(na => na.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure NodeAssignee -> Role relationship
        modelBuilder.Entity<NodeAssigneeEntity>()
            .HasOne(na => na.Role)
            .WithMany(r => r.NodeAssignees)
            .HasForeignKey(na => na.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure NodeAssignee -> User relationship
        modelBuilder.Entity<NodeAssigneeEntity>()
            .HasOne(na => na.User)
            .WithMany(u => u.NodeAssignees)
            .HasForeignKey(na => na.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on NodeAssignee (NodeId, RoleId)
        modelBuilder.Entity<NodeAssigneeEntity>()
            .HasIndex(na => new { na.NodeId, na.RoleId })
            .IsUnique();

        // Configure AuditLogEntry -> Node relationship
        modelBuilder.Entity<AuditLogEntryEntity>()
            .HasOne(a => a.Node)
            .WithMany(n => n.AuditLogEntries)
            .HasForeignKey(a => a.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure AuditLogEntry -> User relationship
        modelBuilder.Entity<AuditLogEntryEntity>()
            .HasOne(a => a.User)
            .WithMany(u => u.AuditLogEntries)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Node -> ChangedTypes (Added/Removed)
        modelBuilder.Entity<NodeAddedTypeEntity>()
            .HasOne(nat => nat.Node)
            .WithMany(n => n.AddedTypes)
            .HasForeignKey(nat => nat.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NodeAddedTypeEntity>()
            .HasOne(nat => nat.Type)
            .WithMany()
            .HasForeignKey(nat => nat.TypeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NodeRemovedTypeEntity>()
            .HasOne(nrt => nrt.Node)
            .WithMany(n => n.RemovedTypes)
            .HasForeignKey(nrt => nrt.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NodeRemovedTypeEntity>()
            .HasOne(nrt => nrt.Type)
            .WithMany()
            .HasForeignKey(nrt => nrt.TypeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Node -> ChangedWorkflows (Added/Removed)
        modelBuilder.Entity<NodeAddedWorkflowEntity>()
            .HasOne(naw => naw.Node)
            .WithMany(n => n.AddedWorkflows)
            .HasForeignKey(naw => naw.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NodeAddedWorkflowEntity>()
            .HasOne(naw => naw.Workflow)
            .WithMany()
            .HasForeignKey(naw => naw.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NodeRemovedWorkflowEntity>()
            .HasOne(nrw => nrw.Node)
            .WithMany(n => n.RemovedWorkflows)
            .HasForeignKey(nrw => nrw.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NodeRemovedWorkflowEntity>()
            .HasOne(nrw => nrw.Workflow)
            .WithMany()
            .HasForeignKey(nrw => nrw.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Node -> ChangedRoles (Added/Removed)
        modelBuilder.Entity<NodeAddedRoleEntity>()
            .HasOne(nar => nar.Node)
            .WithMany(n => n.AddedRoles)
            .HasForeignKey(nar => nar.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NodeAddedRoleEntity>()
            .HasOne(nar => nar.Role)
            .WithMany(r => r.AddedRoleNodes)
            .HasForeignKey(nar => nar.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NodeRemovedRoleEntity>()
            .HasOne(nrr => nrr.Node)
            .WithMany(n => n.RemovedRoles)
            .HasForeignKey(nrr => nrr.NodeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NodeRemovedRoleEntity>()
            .HasOne(nrr => nrr.Role)
            .WithMany()
            .HasForeignKey(nrr => nrr.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Saves all changes made in this context to the underlying database asynchronously.
    /// Audits entity entries and adds audit records to the context.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">Indicates whether AcceptAllChanges should be called after the changes have been saved successfully.</param>
    /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var now = this.timeProvider.GetUtcNow().ToInstant();

        foreach (var entityEntry in this.ChangeTracker.Entries())
        {
            switch (entityEntry.State)
            {
                case EntityState.Added:
                {
                    if (entityEntry.Entity is IHaveLastModifiedDate)
                    {
                        var lastModifiedProp = entityEntry.Property(nameof(IHaveLastModifiedDate.LastModifiedAt));
                        lastModifiedProp.CurrentValue = now;
                    }

                    if (entityEntry.Entity is IHaveCreatedDate)
                    {
                        var createdProp = entityEntry.Property(nameof(IHaveCreatedModifiedDate.CreatedAt));
                        createdProp.CurrentValue = now;
                    }

                    break;
                }
                case EntityState.Modified when entityEntry.Entity is IHaveLastModifiedDate:
                {
                    var lastModifiedProp = entityEntry.Property(nameof(IHaveLastModifiedDate.LastModifiedAt));
                    lastModifiedProp.CurrentValue = now;
                    break;
                }
            }
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
