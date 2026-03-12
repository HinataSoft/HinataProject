using HinataProject.Domain.Dto;
using HinataProject.Persistence;
using HinataProject.Persistence.Entities;
using HinataProject.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HinataProject.Api.Endpoints;

public class UsersEndpoint : IDiscoverableEndpoint
{
    public Task MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("/api/users");

        // List all users (Passive - any authenticated user)
        group.MapGet("/", ListUsers)
            .RequireAuthorization("Passive");

        // Get current authenticated user
        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization("Passive");

        // Get user by ID (Passive - any authenticated user)
        group.MapGet("/{id:guid}", GetUser)
            .RequireAuthorization("Passive");

        // Create new user (Admin only)
        group.MapPost("/", CreateUser)
            .RequireAuthorization("Admin");

        // Delete user (Admin only)
        group.MapDelete("/{id:guid}", DeleteUser)
            .RequireAuthorization("Admin");

        // Update user (Admin only)
        group.MapPut("/{id:guid}", UpdateUser)
            .RequireAuthorization("Admin");

        return Task.CompletedTask;
    }

    private async Task<IResult> GetUser(
        Guid id,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Subject = u.Subject,
                Info = u.Info,
                Rights = u.Rights,
                Roles = u.UserRoles.Select(ur => new UserRoleDto
                {
                    Id = ur.Role!.Id,
                    Name = ur.Role.Name
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (user == null)
        {
            return Results.NotFound(new { error = "UserNotFound" });
        }

        return Results.Ok(user);
    }

    private async Task<IResult> ListUsers(
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var users = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Subject = u.Subject,
                Info = u.Info,
                Rights = u.Rights,
                Roles = u.UserRoles.Select(ur => new UserRoleDto
                {
                    Id = ur.Role!.Id,
                    Name = ur.Role.Name
                }).ToList()
            })
            .ToListAsync(ct);

        return Results.Ok(users);
    }

    private async Task<IResult> GetCurrentUser(
        HinataProjectDataContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var user = await UserUtils.GetCurrentUserAsync(db, httpContext, ct);
        
        var result = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Subject = user.Subject,
            Info = user.Info,
            Rights = user.Rights,
            Roles = user.UserRoles.Select(ur => new UserRoleDto
            {
                Id = ur.Role!.Id,
                Name = ur.Role.Name
            }).ToList()
        };

        return Results.Ok(result);
    }

    private async Task<IResult> CreateUser(
        [FromBody] CreateUserDto dto,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return Results.BadRequest(new { error = "Name is required" });
        }

        if (string.IsNullOrWhiteSpace(dto.Subject))
        {
            return Results.BadRequest(new { error = "Subject is required" });
        }

        var user = new UserEntity
        {
            Name = dto.Name,
            Subject = dto.Subject,
            Info = dto.Info ?? string.Empty,
            Rights = dto.Rights
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var result = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Subject = user.Subject,
            Info = user.Info,
            Rights = user.Rights,
            Roles = new List<UserRoleDto>()
        };

        return Results.Created($"/api/users/{user.Id}", result);
    }

    private async Task<IResult> DeleteUser(
        Guid id,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user == null)
        {
            return Results.NotFound(new { error = "UserNotFound" });
        }

        // Check if this is the last admin user (now using user's Rights property)
        if (user.Rights == Domain.Rights.Admin)
        {
            var adminCount = await db.Users
                .Where(u => u.Rights == Domain.Rights.Admin)
                .CountAsync(ct);

            if (adminCount <= 1)
            {
                return Results.BadRequest(new { error = "CannotDeleteLastAdminUser" });
            }
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    private async Task<IResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserDto dto,
        HinataProjectDataContext db,
        CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user == null)
        {
            return Results.NotFound(new { error = "UserNotFound" });
        }

        // Check if no properties to update
        if (string.IsNullOrWhiteSpace(dto.Name) && string.IsNullOrWhiteSpace(dto.Subject) && dto.Info == null && dto.Rights == null)
        {
            return Results.BadRequest(new { error = "NoPropertiesToUpdate" });
        }

        // Update provided properties
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            user.Name = dto.Name;
        }

        if (!string.IsNullOrWhiteSpace(dto.Subject))
        {
            user.Subject = dto.Subject;
        }

        if (dto.Info != null)
        {
            user.Info = dto.Info;
        }

        if (dto.Rights != null)
        {
            user.Rights = dto.Rights.Value;
        }

        await db.SaveChangesAsync(ct);

        var result = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Subject = user.Subject,
            Info = user.Info,
            Rights = user.Rights,
            Roles = user.UserRoles.Select(ur => new UserRoleDto
            {
                Id = ur.Role!.Id,
                Name = ur.Role.Name
            }).ToList()
        };

        return Results.Ok(result);
    }
}
