using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using HinataProject.Persistence;
using HinataProject.Domain;

namespace HinataProject.Api;

public class RightsRequirement(Rights rights) : IAuthorizationRequirement
{
    public Rights Rights { get; } = rights;
}

public class RightsAuthorizationHandler : AuthorizationHandler<RightsRequirement>
{
    private readonly IDbContextFactory<HinataProjectDataContext> dbContextFactory;

    public RightsAuthorizationHandler(IDbContextFactory<HinataProjectDataContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, RightsRequirement requirement)
    {
        Claim? subjectClaim = context.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier);
        string? subject = subjectClaim?.Value;
        if (String.IsNullOrEmpty(subject))
            return;

        using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(q => q.Subject == subject);
        if (user is null)
            return;

        if (HasEnoughRights(user.Rights, requirement.Rights))
            context.Succeed(requirement);

        return;
    }

    private static bool HasEnoughRights(Rights user, Rights required)
        => required switch
        {
            Rights.Admin => user is Rights.Admin,
            Rights.Active => user is Rights.Admin or Rights.Active,
            Rights.Passive => user is Rights.Admin or Rights.Active or Rights.Passive,
            _ => throw new Exception($"Unknown Rights: {required}"),
        };
}

public static class RightsAuthorizationExtensions
{
    public static IServiceCollection AddRightsAuthorization(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IAuthorizationHandler, RightsAuthorizationHandler>();

        serviceCollection.AddAuthorizationBuilder()
            .AddPolicy("Passive", policy => policy.Requirements.Add(new RightsRequirement(Rights.Passive)))
            .AddPolicy("Active", policy => policy.Requirements.Add(new RightsRequirement(Rights.Active)))
            .AddPolicy("Admin", policy => policy.Requirements.Add(new RightsRequirement(Rights.Admin)));

        return serviceCollection;
    }
}