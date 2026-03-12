using System.Security.Claims;
using HinataProject.Persistence;
using HinataProject.Persistence.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HinataProject.Api.Utils;

public static class UserUtils
{
    /// <summary>
    /// Gets the current user from JWT claims. Throws if user not found in database.
    /// </summary>
    public static async Task<UserEntity> GetCurrentUserAsync(
        HinataProjectDataContext db,
        HttpContext httpContext,
        CancellationToken ct = default)
    {
        var userSubjectClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier) 
            ?? httpContext.User.FindFirst("sub");
        if (userSubjectClaim == null)
            throw new UnauthorizedAccessException("No user ID found in JWT token");

        var user = await db.Users.SingleOrDefaultAsync(q => q.Subject == userSubjectClaim.Value, ct);
        if (user == null)
            throw new UnauthorizedAccessException($"User with subject {userSubjectClaim.Value} not found in database");

        return user;
    }
}
