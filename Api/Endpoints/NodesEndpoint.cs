using HinataProject.Domain.Core;
using HinataProject.Domain.Dto;
using HinataProject.Domain;
using HinataProject.Persistence;
using HinataProject.Persistence.Entities;
using HinataProject.Api.Utils;
using HinataProject.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HinataProject.Api.Endpoints;

public class NodesEndpoint : IDiscoverableEndpoint
{
    private readonly NodeChangeSignal _nodeChangeSignal;

    public NodesEndpoint(NodeChangeSignal nodeChangeSignal)
    {
        _nodeChangeSignal = nodeChangeSignal;
    }

    /// <summary>
    /// Calculates inherited types for a node by traversing up to root.
    /// For Root node: uses its own ChangedTypes as the source.
    /// For other nodes: collects ChangedTypes from all ancestors and calculates effective set.
    /// </summary>
    private async Task<List<TypeEntity>> CalculateInheritedTypesAsync(HinataProjectDataContext db, NodeEntity node, CancellationToken ct)
    {
        var result = new List<TypeEntity>();
        
        // Get all ancestors including current node (for Root, this is the source)
        var ancestors = new List<NodeEntity>();
        var current = node;
        while (current != null)
        {
            ancestors.Add(current);
            if (current.ParentId == null) break; // Reached root
            current = await db.Nodes
                .Include(n => n.AddedTypes).ThenInclude(at => at.Type)
                .Include(n => n.RemovedTypes).ThenInclude(rt => rt.Type)
                .FirstOrDefaultAsync(n => n.Id == current.ParentId, ct);
        }

        // Process ancestors from root to current (reverse order)
        foreach (var ancestor in ancestors.AsEnumerable().Reverse())
        {
            // Get the types added at this level
            var addedTypeIds = ancestor.AddedTypes.Select(at => at.TypeId).ToHashSet();
            
            // Get types removed at this level
            var removedTypeIds = ancestor.RemovedTypes.Select(rt => rt.TypeId).ToHashSet();
            
            // Load the actual Type entities for added types
            var addedTypes = await db.Types
                .Where(t => addedTypeIds.Contains(t.Id))
                .ToListAsync(ct);
            
            // Add types that are not marked as removed
            foreach (var type in addedTypes)
            {
                if (!removedTypeIds.Contains(type.Id))
                {
                    // Only add if not already present
                    if (!result.Any(t => t.Id == type.Id))
                        result.Add(type);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Calculates inherited workflows for a node.
    /// </summary>
    private async Task<List<WorkflowEntity>> CalculateInheritedWorkflowsAsync(HinataProjectDataContext db, NodeEntity node, CancellationToken ct)
    {
        var result = new List<WorkflowEntity>();
        
        var ancestors = new List<NodeEntity>();
        var current = node;
        while (current != null)
        {
            ancestors.Add(current);
            if (current.ParentId == null) break;
            current = await db.Nodes
                .Include(n => n.AddedWorkflows).ThenInclude(aw => aw.Workflow)
                .Include(n => n.RemovedWorkflows).ThenInclude(rw => rw.Workflow)
                .FirstOrDefaultAsync(n => n.Id == current.ParentId, ct);
        }

        foreach (var ancestor in ancestors.AsEnumerable().Reverse())
        {
            var addedWorkflowIds = ancestor.AddedWorkflows.Select(aw => aw.WorkflowId).ToHashSet();
            var removedWorkflowIds = ancestor.RemovedWorkflows.Select(rw => rw.WorkflowId).ToHashSet();
            
            var addedWorkflows = await db.Workflows
                .Include(w => w.States)
                .Where(w => addedWorkflowIds.Contains(w.Id))
                .ToListAsync(ct);
            
            foreach (var workflow in addedWorkflows)
            {
                if (!removedWorkflowIds.Contains(workflow.Id))
                {
                    if (!result.Any(w => w.Id == workflow.Id))
                        result.Add(workflow);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Calculates inherited roles for a node.
    /// </summary>
    private async Task<List<RoleEntity>> CalculateInheritedRolesAsync(HinataProjectDataContext db, NodeEntity node, CancellationToken ct)
    {
        var result = new List<RoleEntity>();
        
        var ancestors = new List<NodeEntity>();
        var current = node;
        while (current != null)
        {
            ancestors.Add(current);
            if (current.ParentId == null) break;
            current = await db.Nodes
                .Include(n => n.AddedRoles).ThenInclude(ar => ar.Role)
                .Include(n => n.RemovedRoles).ThenInclude(rr => rr.Role)
                .FirstOrDefaultAsync(n => n.Id == current.ParentId, ct);
        }

        foreach (var ancestor in ancestors.AsEnumerable().Reverse())
        {
            var addedRoleIds = ancestor.AddedRoles.Select(ar => ar.RoleId).ToHashSet();
            var removedRoleIds = ancestor.RemovedRoles.Select(rr => rr.RoleId).ToHashSet();
            
            var addedRoles = await db.Roles
                .Include(r => r.States)
                .Where(r => addedRoleIds.Contains(r.Id))
                .ToListAsync(ct);
            
            foreach (var role in addedRoles)
            {
                if (!removedRoleIds.Contains(role.Id))
                {
                    if (!result.Any(r => r.Id == role.Id))
                        result.Add(role);
                }
            }
        }

        return result;
    }

    public Task MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("/api/nodes");

        // Get root node ID only (lightweight endpoint for redirects)
        group.MapGet("/root_id", GetRootId)
            .RequireAuthorization("Passive");

        // Create new node
        group.MapPost("/", CreateNode)
            .RequireAuthorization("Active");

        // Get node by ID
        group.MapGet("/{id:guid}", GetNode)
            .RequireAuthorization("Passive");

        // List children of a node
        group.MapGet("/{id:guid}/children", ListChildren)
            .RequireAuthorization("Passive");

        // List nodes assigned to current user
        group.MapGet("/assigned-to-me", GetAssignedToMe)
            .RequireAuthorization("Passive");

        // Long-poll for changes in assigned nodes
        group.MapGet("/assigned-to-me/poll", PollAssignedToMe)
            .RequireAuthorization("Passive");

        // Update node properties
        group.MapPut("/{id:guid}", UpdateNode)
            .RequireAuthorization("Active");

        // Update guardrails (Admin only)
        group.MapPut("/{id:guid}/guardrails", UpdateGuardrails)
            .RequireAuthorization("Admin");

        // Delete node
        group.MapDelete("/{id:guid}", DeleteNode)
            .RequireAuthorization("Admin");

        // Set ChangedTypes
        group.MapPut("/{id:guid}/types", SetTypes)
            .RequireAuthorization("Admin");

        // Set ChangedWorkflows
        group.MapPut("/{id:guid}/workflows", SetWorkflows)
            .RequireAuthorization("Admin");

        // Set ChangedRoles
        group.MapPut("/{id:guid}/roles", SetRoles)
            .RequireAuthorization("Admin");

        // Set State
        group.MapPut("/{id:guid}/state", SetState)
            .RequireAuthorization("Passive");

        // Set Workflow (for stateful nodes)
        group.MapPut("/{id:guid}/workflow", SetNodeWorkflow)
            .RequireAuthorization("Admin");

        // Get Assignees
        group.MapGet("/{id:guid}/assignees", GetAssignees)
            .RequireAuthorization("Passive");

        // Set Assignees
        group.MapPut("/{id:guid}/assignees", SetAssignees)
            .RequireAuthorization("Admin");

        // List Comments
        group.MapGet("/{nodeId:guid}/comments", ListComments)
            .RequireAuthorization("Passive");

        // Add Comment
        group.MapPost("/{nodeId:guid}/comments", AddComment)
            .RequireAuthorization("Passive");

        return Task.CompletedTask;
    }

    private async Task<IResult> GetRootId(
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var root = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentId == null)
            .Select(n => n.Id)
            .FirstOrDefaultAsync(ct);

        if (root == default)
            return Results.NotFound();

        return Results.Ok(new { id = root });
    }

    private async Task<IResult> CreateNode(
        [FromBody] CreateNodeDto dto,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var parent = await db.Nodes.FindAsync(new object[] { dto.ParentId }, ct);
        if (parent == null)
            return Results.NotFound(new { error = "ParentNotFound" });

        // Load type with its default workflow
        var type = await db.Types
            .Include(t => t.DefaultWorkflow)
            .FirstOrDefaultAsync(t => t.Id == dto.TypeId, ct);
        
        if (type == null)
            return Results.BadRequest(new { error = "TypeNotAllowed" });

        // Check if type is in parent's inherited types
        var parentWithChanges = await db.Nodes
            .Include(n => n.AddedTypes).ThenInclude(at => at.Type)
            .Include(n => n.RemovedTypes)
            .FirstOrDefaultAsync(n => n.Id == dto.ParentId, ct);
        
        if (parentWithChanges != null)
        {
            var inheritedTypes = await CalculateInheritedTypesAsync(db, parentWithChanges, ct);
            if (!inheritedTypes.Any(t => t.Id == dto.TypeId))
                return Results.BadRequest(new { error = "TypeNotAllowed" });
        }

        // Get the next PublicId (auto-increment)
        var maxPublicId = await db.Nodes.MaxAsync(n => (int?)n.PublicId, ct) ?? 0;

        var node = new NodeEntity
        {
            ParentId = dto.ParentId,
            TypeId = dto.TypeId,
            PublicId = maxPublicId + 1,
            Manifest = dto.Manifest ?? string.Empty,
            Caption = dto.Caption ?? string.Empty,
            Description = dto.Description ?? string.Empty,
            Guardrails = dto.Guardrails ?? string.Empty
        };

        // If stateful type, workflow is required
        if (type.Kind == TypeKind.Stateful)
        {
            WorkflowEntity? workflow = null;

            // Use explicitly provided workflowId
            if (dto.WorkflowId != null)
            {
                workflow = await db.Workflows.FindAsync(new object[] { dto.WorkflowId.Value }, ct);
                if (workflow == null)
                    return Results.BadRequest(new { error = "WorkflowNotAllowed" });
            }
            // Fall back to type's default workflow
            else if (type.DefaultWorkflow != null)
            {
                workflow = type.DefaultWorkflow;
            }

            // If still no workflow found, return error
            if (workflow == null)
                return Results.BadRequest(new { error = "WorkflowRequired" });

            node.WorkflowId = workflow.Id;
            node.StateId = workflow.DefaultStateId;
        }

        db.Nodes.Add(node);
        await db.SaveChangesAsync(ct);

        // Notify assignees of the new node
        _ = _nodeChangeSignal.NotifyForNode(node.Id);

        return Results.Created($"/nodes/{node.Id}", new { id = node.Id });
    }

    private async Task<IResult> GetNode(
        Guid id,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var node = await db.Nodes
            .Include(n => n.Type)
            .Include(n => n.Workflow)
            .Include(n => n.State)
            .Include(n => n.AddedTypes).ThenInclude(at => at.Type)
            .Include(n => n.RemovedTypes)
            .Include(n => n.AddedWorkflows).ThenInclude(aw => aw.Workflow).ThenInclude(w => w.States)
            .Include(n => n.RemovedWorkflows)
            .Include(n => n.AddedRoles).ThenInclude(ar => ar.Role).ThenInclude(r => r.States)
            .Include(n => n.RemovedRoles).ThenInclude(rr => rr.Role)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        // Calculate inherited properties
        var inheritedTypes = await CalculateInheritedTypesAsync(db, node, ct);
        var inheritedWorkflows = await CalculateInheritedWorkflowsAsync(db, node, ct);
        var inheritedRoles = await CalculateInheritedRolesAsync(db, node, ct);

        // For stateful nodes, calculate the current assignee
        object? assignee = null;
        if (node.Type != null && node.Type.Kind == TypeKind.Stateful && node.WorkflowId != null && node.StateId != null)
        {
            var currentState = await db.States
                .Include(s => s.Roles)
                .FirstOrDefaultAsync(s => s.Id == node.StateId, ct);
            
            if (currentState != null)
            {
                // Find role that corresponds to current state
                var stateRole = currentState.Roles.FirstOrDefault();
                if (stateRole != null)
                {
                    var nodeAssignee = await db.NodeAssignees
                        .Include(na => na.User)
                        .FirstOrDefaultAsync(na => na.NodeId == id && na.RoleId == stateRole.Id, ct);
                    
                    if (nodeAssignee?.User != null)
                    {
                        assignee = new { nodeAssignee.User.Id, nodeAssignee.User.Name };
                    }
                }
            }
        }

        // Get Changed* for structural nodes
        object? changedTypes = null;
        object? changedWorkflows = null;
        object? changedRoles = null;
        
        if (node.Type != null && node.Type.Kind == TypeKind.Structural)
        {
            changedTypes = new
            {
                Added = node.AddedTypes.Select(at => new { at.TypeId, at.Type?.Name, at.Type?.Kind, at.Type?.Color, at.Type?.DefaultWorkflowId }).ToList(),
                Removed = node.RemovedTypes.Select(rt => new { rt.TypeId }).ToList()
            };
            changedWorkflows = new
            {
                Added = node.AddedWorkflows.Select(aw => new { 
                    aw.WorkflowId, 
                    aw.Workflow?.Name,
                    States = aw.Workflow?.States?.Select(s => new { stateId = s.Id, s.Name, s.IsFinalSuccess, s.IsFinalFailure }).ToList()
                }).ToList(),
                Removed = node.RemovedWorkflows.Select(rw => new { rw.WorkflowId }).ToList()
            };
            changedRoles = new
            {
                Added = node.AddedRoles.Select(ar => new { ar.RoleId, ar.Role?.Name, States = ar.Role?.States?.Select(s => new { s.Id, s.Name }).ToList() }).ToList(),
                Removed = node.RemovedRoles.Select(rr => new { rr.RoleId }).ToList()
            };
        }

        return Results.Ok(new
        {
            node.Id,
            node.ParentId,
            node.PublicId,
            node.Manifest,
            node.Caption,
            node.Description,
            node.Guardrails,
            Type = node.Type != null ? new { node.Type.Id, node.Type.Name, node.Type.Kind, node.Type.Color } : null,
            Workflow = node.Workflow != null ? new { node.Workflow.Id, node.Workflow.Name } : null,
            State = node.State != null ? new { node.State.Id, node.State.Name, node.State.IsFinalSuccess, node.State.IsFinalFailure } : null,
            InheritedTypes = inheritedTypes.Select(t => new { t.Id, t.Name, t.Kind, t.Color, t.DefaultWorkflowId }).ToList(),
            InheritedWorkflows = inheritedWorkflows.Select(w => new { w.Id, w.Name, States = w.States?.Select(s => new { s.Id, s.Name }).ToList() }).ToList(),
            InheritedRoles = inheritedRoles.Select(r => new { r.Id, r.Name, States = r.States?.Select(s => new { s.Id, s.Name }).ToList() }).ToList(),
            ChangedTypes = changedTypes,
            ChangedWorkflows = changedWorkflows,
            ChangedRoles = changedRoles,
            Assignee = assignee
        });
    }

    private async Task<IResult> ListChildren(
        Guid id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        HinataProjectDataContext db = null!,
        CancellationToken ct = default)
    {
        var parent = await db.Nodes.FindAsync(new object[] { id }, ct);
        if (parent == null)
            return Results.NotFound(new { error = "ParentNotFound" });

        var query = db.Nodes
            .Where(n => n.ParentId == id)
            .Include(n => n.Type)
            .Include(n => n.State)
            .Skip(skip)
            .Take(take);

        var totalCount = await db.Nodes.CountAsync(n => n.ParentId == id, ct);
        
        var children = await query.ToListAsync(ct);

        var items = children.Select(c => new
        {
            c.Id,
            c.PublicId,
            c.Caption,
            Type = c.Type != null ? new { c.Type.Id, c.Type.Name, c.Type.Color } : null,
            State = c.State != null ? new { c.State.Id, c.State.Name } : null
        }).Cast<object>().ToList();

        return Results.Ok(new PagedResult<object> { Items = items, TotalCount = totalCount });
    }

    private async Task<IResult> GetAssignedToMe(
        HinataProjectDataContext db,
        HttpContext httpContext,
        CancellationToken ct,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        // Get current user from JWT token
        var user = await UserUtils.GetCurrentUserAsync(db, httpContext, ct);

        // Single EF query: find all nodes where:
        // 1. Node is stateful (has a State)
        // 2. The current state's Role (State.Roles) has an assignee for this user
        var baseQuery = db.Nodes
            .Include(n => n.Type)
            .Include(n => n.State)
                .ThenInclude(s => s!.Roles)
            .Where(n => n.StateId != null)
            .Where(n => n.Type != null && n.Type.Kind == TypeKind.Stateful)
            .Where(n => n.State!.Roles.Any(role => 
                db.NodeAssignees.Any(na => 
                    na.NodeId == n.Id && 
                    na.RoleId == role.Id && 
                    na.UserId == user.Id)));

        var totalCount = await baseQuery.CountAsync(ct);
        
        var nodes = await baseQuery
            .OrderBy(n => n.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        var items = nodes.Select(n => new
        {
            n.Id,
            n.PublicId,
            n.Caption,
            Type = n.Type != null ? new { n.Type.Id, n.Type.Name, n.Type.Color } : null,
            State = n.State != null ? new { n.State.Id, n.State.Name } : null
        }).Cast<object>().ToList();

        return Results.Ok(new PagedResult<object> { Items = items, TotalCount = totalCount });
    }

    private async Task<IResult> PollAssignedToMe(
        HinataProjectDataContext db,
        HttpContext httpContext,
        NodeChangeSignal nodeChangeSignal,
        CancellationToken ct,
        [FromQuery] DateTime? since = null,
        [FromQuery] int timeout = 30)
    {
        var user = await UserUtils.GetCurrentUserAsync(db, httpContext, ct);

        // Cap timeout to prevent abuse
        timeout = Math.Clamp(timeout, 1, 60);

        // Wait for a signal or timeout
        var signaled = await nodeChangeSignal.WaitAsync(user.Id.ToString(), timeout, ct);

        // Now check if anything actually changed since 'since'
        DateTime? sinceTime = since?.ToUniversalTime();

        // Get the max LastModifiedAt of nodes assigned to this user
        var maxLastModified = await db.NodeAssignees
            .Where(na => na.UserId == user.Id)
            .Select(na => na.Node!.LastModifiedAt)
            .MaxAsync(ct);

        var hasChanged = (!sinceTime.HasValue || maxLastModified.ToDateTimeUtc() > sinceTime.Value);

        return Results.Ok(new
        {
            changed = hasChanged,
            serverTimestamp = DateTime.UtcNow.ToString("O")
        });
    }

    private async Task<IResult> UpdateNode(
        Guid id,
        [FromBody] UpdateNodeDto dto,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var node = await db.Nodes.FindAsync(new object[] { id }, ct);
        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        if (dto.Manifest == null && dto.Caption == null && dto.Description == null)
            return Results.BadRequest(new { error = "NoPropertiesToUpdate" });

        if (dto.Manifest != null) node.Manifest = dto.Manifest;
        if (dto.Caption != null) node.Caption = dto.Caption;
        if (dto.Description != null) node.Description = dto.Description;

        await db.SaveChangesAsync(ct);

        // Notify all assignees that the node was updated
        _ = _nodeChangeSignal.NotifyForNode(id);

        return Results.Ok();
    }

    private async Task<IResult> UpdateGuardrails(
        Guid id,
        [FromBody] UpdateGuardrailsDto dto,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var node = await db.Nodes.FindAsync(new object[] { id }, ct);
        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        node.Guardrails = dto.Guardrails;
        await db.SaveChangesAsync(ct);

        _ = _nodeChangeSignal.NotifyForNode(id);

        return Results.Ok();
    }

    private async Task<IResult> DeleteNode(
        Guid id,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var node = await db.Nodes.FindAsync(new object[] { id }, ct);
        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        // Cannot delete root node (check if parent is null and has specific publicId)
        if (node.ParentId == null && node.PublicId == 1)
            return Results.BadRequest(new { error = "CannotDeleteRoot" });

        // Delete children recursively
        await DeleteNodeRecursive(id, db, ct);

        return Results.NoContent();
    }

    private async Task DeleteNodeRecursive(Guid nodeId, HinataProjectDataContext db, CancellationToken ct)
    {
        var children = await db.Nodes.Where(n => n.ParentId == nodeId).ToListAsync(ct);
        foreach (var child in children)
        {
            await DeleteNodeRecursive(child.Id, db, ct);
        }

        var node = await db.Nodes.FindAsync(new object[] { nodeId }, ct);
        if (node != null)
        {
            db.Nodes.Remove(node);
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<IResult> SetTypes(
        Guid id,
        [FromBody] SetChangedTypesDto dto,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var node = await db.Nodes
            .Include(n => n.Type)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        if (node.Type == null || node.Type.Kind != TypeKind.Structural)
            return Results.BadRequest(new { error = "NodeNotStructural" });

        // First, remove all existing added types for this node (to handle updates properly)
        var existingAddedTypes = await db.NodeAddedTypes
            .Where(nat => nat.NodeId == id)
            .ToListAsync(ct);
        db.NodeAddedTypes.RemoveRange(existingAddedTypes);
        await db.SaveChangesAsync(ct);

        // Also remove all existing removed types for this node (they will be re-added based on current state)
        var existingRemovedTypes = await db.NodeRemovedTypes
            .Where(nrt => nrt.NodeId == id)
            .ToListAsync(ct);
        db.NodeRemovedTypes.RemoveRange(existingRemovedTypes);
        await db.SaveChangesAsync(ct);

        // Handle added types
        if (dto.Added != null)
        {
            foreach (var typeDto in dto.Added)
            {
                TypeEntity type;

                // Check if TypeId is provided - if so, try to find existing type
                if (typeDto.TypeId.HasValue)
                {
                    var existingType = await db.Types.FindAsync(new object[] { typeDto.TypeId.Value }, ct);
                    if (existingType != null)
                    {
                        // Update existing type
                        existingType.Kind = typeDto.Kind;
                        existingType.Name = typeDto.Name;
                        existingType.Color = typeDto.Color;
                        existingType.DefaultWorkflowId = typeDto.DefaultWorkflowId;
                        type = existingType;
                    }
                    else
                    {
                        // TypeId provided but not found - create new type
                        type = new TypeEntity
                        {
                            Kind = typeDto.Kind,
                            Name = typeDto.Name,
                            Color = typeDto.Color,
                            DefaultWorkflowId = typeDto.DefaultWorkflowId,
                        };
                        db.Types.Add(type);
                    }
                }
                else
                {
                    // No TypeId - create new type
                    type = new TypeEntity
                    {
                        Kind = typeDto.Kind,
                        Name = typeDto.Name,
                        Color = typeDto.Color,
                        DefaultWorkflowId = typeDto.DefaultWorkflowId,
                    };
                    db.Types.Add(type);
                }

                await db.SaveChangesAsync(ct);

                db.NodeAddedTypes.Add(new NodeAddedTypeEntity
                {
                    NodeId = id,
                    TypeId = type.Id
                });
            }
        }

        // Handle removed types
        if (dto.Removed != null)
        {
            foreach (var typeId in dto.Removed)
            {
                db.NodeRemovedTypes.Add(new NodeRemovedTypeEntity
                {
                    NodeId = id,
                    TypeId = typeId
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private async Task<IResult> SetWorkflows(
        Guid id,
        [FromBody] SetChangedWorkflowsDto dto,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var node = await db.Nodes
            .Include(n => n.Type)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        if (node.Type == null || node.Type.Kind != TypeKind.Structural)
            return Results.BadRequest(new { error = "NodeNotStructural" });

        // First, remove all existing added workflows for this node (to handle updates properly)
        var existingAddedWorkflows = await db.NodeAddedWorkflows
            .Where(naw => naw.NodeId == id)
            .ToListAsync(ct);
        db.NodeAddedWorkflows.RemoveRange(existingAddedWorkflows);
        await db.SaveChangesAsync(ct);

        if (dto.Added != null)
        {
            foreach (var workflowDto in dto.Added)
            {
                WorkflowEntity workflow;

                // Check if workflowId is provided - if so, try to find existing workflow
                if (!string.IsNullOrEmpty(workflowDto.WorkflowId))
                {
                    var existingWorkflow = await db.Workflows.FindAsync(new object[] { Guid.Parse(workflowDto.WorkflowId) }, ct);
                    if (existingWorkflow != null)
                    {
                        // Update existing workflow
                        existingWorkflow.Name = workflowDto.Name;
                        workflow = existingWorkflow;
                    }
                    else
                    {
                        // Create new workflow if not found
                        workflow = new WorkflowEntity { Name = workflowDto.Name };
                        db.Workflows.Add(workflow);
                    }
                }
                else
                {
                    // No workflowId - create new workflow
                    workflow = new WorkflowEntity { Name = workflowDto.Name };
                    db.Workflows.Add(workflow);
                }

                await db.SaveChangesAsync(ct);

                // Get all existing states for this workflow (before modifications)
                var existingStates = await db.States
                    .Where(s => s.WorkflowId == workflow.Id)
                    .ToListAsync(ct);

                // Collect all state IDs from the DTO (both existing and new)
                var dtoStateIds = workflowDto.States
                    .Where(s => !string.IsNullOrEmpty(s.StateId))
                    .Select(s => Guid.Parse(s.StateId!))
                    .ToHashSet();

                // Delete all states that are no longer in the collection
                foreach (var stateToRemove in existingStates)
                {
                    if (!dtoStateIds.Contains(stateToRemove.Id))
                    {
                        db.States.Remove(stateToRemove);
                    }
                }
                await db.SaveChangesAsync(ct);

                // Process states: edit existing ones and create new ones
                StateEntity? defaultState = null;
                StateEntity? explicitDefaultState = null;

                foreach (var stateDto in workflowDto.States)
                {
                    StateEntity? state = null;

                    // Check if stateId is provided and not empty - try to find existing state
                    if (!string.IsNullOrEmpty(stateDto.StateId))
                    {
                        try
                        {
                            var stateIdGuid = Guid.Parse(stateDto.StateId);
                            var existingState = await db.States.FindAsync(new object[] { stateIdGuid }, ct);
                            if (existingState != null)
                            {
                                // Update existing state
                                existingState.Name = stateDto.Name;
                                existingState.IsFinalSuccess = stateDto.IsFinalSuccess;
                                existingState.IsFinalFailure = stateDto.IsFinalFailure;
                                state = existingState;
                            }
                        }
                        catch (FormatException)
                        {
                            // Invalid GUID format - treat as new state
                            state = null;
                        }
                    }

                    // If no existing state found (or stateId was empty/null/invalid), create new state
                    if (state == null)
                    {
                        state = new StateEntity
                        {
                            Name = stateDto.Name,
                            IsFinalSuccess = stateDto.IsFinalSuccess,
                            IsFinalFailure = stateDto.IsFinalFailure,
                            WorkflowId = workflow.Id
                        };
                        db.States.Add(state);
                    }

                    // Track the first state as default if no default is set yet
                    if (defaultState == null)
                    {
                        defaultState = state;
                    }

                    // Check if this is the explicitly set default state
                    if (workflowDto.DefaultStateId.HasValue && !string.IsNullOrEmpty(stateDto.StateId))
                    {
                        try
                        {
                            if (Guid.Parse(stateDto.StateId) == workflowDto.DefaultStateId.Value)
                            {
                                explicitDefaultState = state;
                            }
                        }
                        catch (FormatException)
                        {
                            // Invalid GUID format, ignore
                        }
                    }
                }
                await db.SaveChangesAsync(ct);
                
                // Set default state: explicit first, then first state if none
                if (explicitDefaultState != null)
                {
                    workflow.DefaultStateId = explicitDefaultState.Id;
                }
                else if (workflow.DefaultStateId == null && defaultState != null)
                {
                    workflow.DefaultStateId = defaultState.Id;
                }

                db.NodeAddedWorkflows.Add(new NodeAddedWorkflowEntity
                {
                    NodeId = id,
                    WorkflowId = workflow.Id
                });
            }
        }

        if (dto.Removed != null)
        {
            foreach (var workflowId in dto.Removed)
            {
                db.NodeRemovedWorkflows.Add(new NodeRemovedWorkflowEntity
                {
                    NodeId = id,
                    WorkflowId = workflowId
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private async Task<IResult> SetRoles(
        Guid id,
        [FromBody] SetChangedRolesDto dto,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var node = await db.Nodes
            .Include(n => n.Type)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        if (node.Type == null || node.Type.Kind != TypeKind.Structural)
            return Results.BadRequest(new { error = "NodeNotStructural" });

        // First, remove all existing added roles for this node (to handle updates properly)
        var existingAddedRoles = await db.NodeAddedRoles
            .Where(nar => nar.NodeId == id)
            .ToListAsync(ct);
        db.NodeAddedRoles.RemoveRange(existingAddedRoles);
        await db.SaveChangesAsync(ct);

        // Also remove all existing removed roles for this node (they will be re-added based on current state)
        var existingRemovedRoles = await db.NodeRemovedRoles
            .Where(nrr => nrr.NodeId == id)
            .ToListAsync(ct);
        db.NodeRemovedRoles.RemoveRange(existingRemovedRoles);
        await db.SaveChangesAsync(ct);

        if (dto.Added != null)
        {
            foreach (var roleDto in dto.Added)
            {
                RoleEntity role;

                // Check if RoleId is provided - if so, try to find existing role
                if (roleDto.RoleId.HasValue)
                {
                    var existingRole = await db.Roles
                        .Include(r => r.States)
                        .FirstOrDefaultAsync(r => r.Id == roleDto.RoleId.Value, ct);

                    if (existingRole != null)
                    {
                        // Update existing role
                        existingRole.Name = roleDto.Name;
                        role = existingRole;
                    }
                    else
                    {
                        // RoleId provided but not found - create new role
                        role = new RoleEntity
                        {
                            Name = roleDto.Name
                        };
                        db.Roles.Add(role);
                    }
                }
                else
                {
                    // No RoleId - create new role
                    role = new RoleEntity
                    {
                        Name = roleDto.Name
                    };
                    db.Roles.Add(role);
                }

                await db.SaveChangesAsync(ct);

                // Update states for the role
                // Clear existing states from the role's navigation property and re-add
                role.States.Clear();
                
                // Add states to the role
                if (roleDto.States != null && roleDto.States.Count > 0)
                {
                    foreach (var stateId in roleDto.States)
                    {
                        var state = await db.States.FindAsync(new object[] { stateId }, ct);
                        if (state != null)
                        {
                            role.States.Add(state);
                        }
                    }
                }

                db.NodeAddedRoles.Add(new NodeAddedRoleEntity
                {
                    NodeId = id,
                    RoleId = role.Id
                });
            }
        }

        if (dto.Removed != null)
        {
            foreach (var roleId in dto.Removed)
            {
                db.NodeRemovedRoles.Add(new NodeRemovedRoleEntity
                {
                    NodeId = id,
                    RoleId = roleId
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private async Task<IResult> SetState(
        Guid id,
        [FromBody] SetStateDto dto,
        HinataProjectDataContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var node = await db.Nodes
            .Include(n => n.Type)
            .Include(n => n.Workflow)
            .Include(n => n.State)
                .ThenInclude(s => s!.Roles)
            .Include(n => n.Assignees)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        if (node.Type == null || node.Type.Kind != TypeKind.Stateful)
            return Results.BadRequest(new { error = "NodeNotStateful" });

        var targetState = await db.States.FindAsync(new object[] { dto.TargetStateId }, ct);
        if (targetState == null || targetState.WorkflowId != node.WorkflowId)
            return Results.BadRequest(new { error = "StateNotInWorkflow" });

        // Get current user from JWT
        var currentUser = await UserUtils.GetCurrentUserAsync(db, httpContext, ct);

        // Check if user is admin
        var isAdmin = currentUser.Rights == Domain.Rights.Admin;

        // Authorization: Admin can change state to anything
        // OR current user must be the current assignee (person the node is assigned to)
        if (!isAdmin)
        {
            // Find the role that corresponds to current state
            var currentStateRole = node.State?.Roles.FirstOrDefault();
            if (currentStateRole == null)
                return Results.BadRequest(new { error = "NoRoleForCurrentState" });

            // Check if current user is assigned to this role on this node
            var isCurrentAssignee = node.Assignees.Any(a =>
                a.RoleId == currentStateRole.Id && a.UserId == currentUser.Id);

            if (!isCurrentAssignee)
                return Results.Forbid();
        }

        node.StateId = dto.TargetStateId;
        await db.SaveChangesAsync(ct);

        // Notify all assignees that the node state changed
        _ = _nodeChangeSignal.NotifyForNode(id);

        return Results.Ok();
    }

    private async Task<IResult> SetNodeWorkflow(
        Guid id,
        [FromBody] SetWorkflowDto dto,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var node = await db.Nodes
            .Include(n => n.Type)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        if (node.Type == null || node.Type.Kind != TypeKind.Stateful)
            return Results.BadRequest(new { error = "NodeNotStateful" });

        // Validate that the workflow exists in inherited workflows
        var inheritedWorkflows = await CalculateInheritedWorkflowsAsync(db, node, ct);
        if (!inheritedWorkflows.Any(w => w.Id == dto.WorkflowId))
            return Results.BadRequest(new { error = "WorkflowNotAllowed" });

        var workflow = await db.Workflows.FindAsync(new object[] { dto.WorkflowId }, ct);
        if (workflow == null)
            return Results.BadRequest(new { error = "WorkflowNotAllowed" });

        node.WorkflowId = dto.WorkflowId;
        
        // Set the default state for the new workflow
        if (workflow.DefaultStateId != null)
        {
            node.StateId = workflow.DefaultStateId;
        }

        await db.SaveChangesAsync(ct);

        // Notify all assignees that the node workflow changed
        _ = _nodeChangeSignal.NotifyForNode(id);

        return Results.Ok();
    }

    private async Task<IResult> GetAssignees(
        Guid id,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var node = await db.Nodes
            .Include(n => n.Type)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        if (node.Type == null || node.Type.Kind != TypeKind.Stateful)
            return Results.BadRequest(new { error = "NodeNotStateful" });

        var assignees = await db.NodeAssignees
            .Where(na => na.NodeId == id)
            .Include(na => na.Role)
            .Include(na => na.User)
            .ToListAsync(ct);

        var result = assignees.ToDictionary(a => a.RoleId, a => a.UserId);
        return Results.Ok(result);
    }

    private async Task<IResult> SetAssignees(
        Guid id,
        [FromBody] SetAssigneesDto dto,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var node = await db.Nodes
            .Include(n => n.Type)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        if (node.Type == null || node.Type.Kind != TypeKind.Stateful)
            return Results.BadRequest(new { error = "NodeNotStateful" });

        // Remove existing assignees
        var existing = await db.NodeAssignees.Where(na => na.NodeId == id).ToListAsync(ct);
        db.NodeAssignees.RemoveRange(existing);

        // Add new assignees
        foreach (var (roleId, userId) in dto.Assignees)
        {
            var role = await db.Roles.FindAsync(new object[] { roleId }, ct);
            if (role == null)
                return Results.BadRequest(new { error = "RoleNotAllowed" });

            var user = await db.Users.FindAsync(new object[] { userId }, ct);
            if (user == null)
                return Results.BadRequest(new { error = "UserNotFound" });

            db.NodeAssignees.Add(new NodeAssigneeEntity
            {
                NodeId = id,
                RoleId = roleId,
                UserId = userId
            });
        }

        await db.SaveChangesAsync(ct);

        // Notify all assignees (both old and new) that assignments changed
        _ = _nodeChangeSignal.NotifyForNode(id);

        return Results.Ok();
    }

    private async Task<IResult> ListComments(
        Guid nodeId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string ordering = "asc",
        HinataProjectDataContext db = null!,
        CancellationToken ct = default)
    {
        var node = await db.Nodes.FindAsync(new object[] { nodeId }, ct);
        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        var baseQuery = db.Comments.Where(c => c.NodeId == nodeId);
        
        var totalCount = await baseQuery.CountAsync(ct);
        
        var comments = ordering == "desc"
            ? baseQuery.OrderByDescending(c => c.CreatedAt).Skip(skip).Take(take)
            : baseQuery.OrderBy(c => c.CreatedAt).Skip(skip).Take(take);

        var items = await comments.Include(c => c.User).ToListAsync(ct);

        var result = items.Select(c => new
        {
            c.Id,
            c.Text,
            User = c.User != null ? new { c.User.Id, c.User.Name } : null,
            c.CreatedAt
        }).Cast<object>().ToList();

        return Results.Ok(new PagedResult<object>
        {
            Items = result,
            TotalCount = totalCount
        });
    }

    private async Task<IResult> AddComment(
        Guid nodeId,
        [FromBody] AddCommentDto dto,
        HinataProjectDataContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var node = await db.Nodes.FindAsync(new object[] { nodeId }, ct);
        if (node == null)
            return Results.NotFound(new { error = "NodeNotFound" });

        if (string.IsNullOrWhiteSpace(dto.Text))
            return Results.BadRequest(new { error = "EmptyComment" });

        // Get user from JWT claims
        var user = await UserUtils.GetCurrentUserAsync(db, httpContext, ct);

        var comment = new CommentEntity
        {
            NodeId = nodeId,
            UserId = user.Id,
            Text = dto.Text
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        // Notify all assignees that a comment was added
        _ = _nodeChangeSignal.NotifyForNode(nodeId);

        return Results.Created($"/nodes/{nodeId}/comments/{comment.Id}", new { id = comment.Id });
    }
}
