using HinataProject.Domain;
using HinataProject.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace HinataProject.Persistence.Utils;

public static class RegisterPersistenceFactoryExtensions
{
    public static IServiceCollection ConfigurePersistence(this IServiceCollection serviceCollection,
                                                          IConfiguration configuration)
    {
        
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var log = configuration.GetValue<bool>($"ConnectionStringsLogSettings:DbContext:EnableLog", false);
        var sensitiveDataLog = configuration.GetValue<bool>(
            $"ConnectionStringsLogSettings:DbContext:EnableSensitiveDataLogging",
            false);
        
        var connectionString = configuration.GetConnectionString("DbContext");

        if (connectionString is null)
            throw new InvalidOperationException("Connection string is not configured");
        
        serviceCollection.DoPostgresRegistration(
            connectionString,
            enableLog: log,
            enableSensitiveDataLogging: sensitiveDataLog,
            loggerFactory: loggerFactory);

        return serviceCollection;
    }

    public static void ConfigureBuilder(this DbContextOptionsBuilder optionsBuilder,
                                        string connectionString,
                                        bool enableLog = false,
                                        bool enableSensitiveDataLogging = false,
                                        ILoggerFactory? loggerFactory = null)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
        optionsBuilder.UseNpgsql(
            connectionString,
            q =>
            {
                q.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
                q.UseNodaTime();
            });
        
        if (enableLog)
        {
            optionsBuilder.UseLoggerFactory(loggerFactory);
            if (enableSensitiveDataLogging)
                optionsBuilder.EnableSensitiveDataLogging();
        }
    }
    

    public static void DoPostgresRegistration(this IServiceCollection serviceCollection,
                                             string connectionString,
                                             bool enableLog = false,
                                             bool enableSensitiveDataLogging = false,
                                             ILoggerFactory? loggerFactory = null)
        => serviceCollection.AddDbContextFactory<HinataProjectDataContext>(
            optionsBuilder =>
            {
                optionsBuilder.ConfigureBuilder(
                    connectionString: connectionString,
                    enableLog: enableLog && loggerFactory is not null,
                    enableSensitiveDataLogging: enableSensitiveDataLogging && enableLog,
                    loggerFactory: enableLog ? loggerFactory : null);
            });

    public static async Task PrepareDataProviders(this IServiceProvider serviceCollection)
    {
        var service = serviceCollection.GetRequiredService<IDbContextFactory<HinataProjectDataContext>>();
        await using var ctx = await service.CreateDbContextAsync();
        var factory = serviceCollection.GetRequiredService<ILoggerFactory>();
        var logger = factory.CreateLogger<IServiceProvider>();

        logger.LogInformation("Pending migrations start:");

        foreach (var pendingMigration in await ctx.Database.GetPendingMigrationsAsync())
        {
            Console.WriteLine(pendingMigration);
        }
        logger.LogInformation("Pending migrations stop:");

        logger.LogInformation("MigrateAsync start:");
        await ctx.Database.MigrateAsync();
        logger.LogInformation("MigrateAsync stop:");

        // Seed initial data
        await SeedInitialDataAsync(ctx, logger);
    }

    private static async Task SeedInitialDataAsync(HinataProjectDataContext ctx, ILogger logger)
    {
        var now = SystemClock.Instance.GetCurrentInstant();

        logger.LogInformation("Seeding initial data start:");

        // Create Superadmin role if it doesn't exist
        if (!await ctx.Roles.AnyAsync())
        {
            logger.LogInformation("Creating Superadmin role...");
            var superadminRole = new RoleEntity
            {
                Id = Guid.NewGuid(),
                Name = "Superadmin",
                CreatedAt = now,
                LastModifiedAt = now
            };
            ctx.Roles.Add(superadminRole);
            await ctx.SaveChangesAsync();
            logger.LogInformation("Superadmin role created with ID: {RoleId}", superadminRole.Id);
        }

        // Create user with Superadmin role if no users exist
        if (!await ctx.Users.AnyAsync())
        {
            var superadminRole = await ctx.Roles.FirstAsync(r => r.Name == "Superadmin");
            logger.LogInformation("Creating default user...");
            var defaultUser = new UserEntity
            {
                Id = Guid.NewGuid(),
                Name = "Default User",
                Subject = "dummy-subject",
                Rights = Rights.Admin,
                CreatedAt = now,
                LastModifiedAt = now
            };
            ctx.Users.Add(defaultUser);
            await ctx.SaveChangesAsync();

            // Assign user to Superadmin role
            var userRole = new UserRoleEntity
            {
                Id = Guid.NewGuid(),
                UserId = defaultUser.Id,
                RoleId = superadminRole.Id,
                CreatedAt = now,
                LastModifiedAt = now
            };
            ctx.UserRoles.Add(userRole);
            await ctx.SaveChangesAsync();
            logger.LogInformation("Default user created with ID: {UserId}, Subject: dummy-subject", defaultUser.Id);
        }

        // Create Folder type if no types exist
        if (!await ctx.Types.AnyAsync())
        {
            logger.LogInformation("Creating Folder type...");
            var folderType = new TypeEntity
            {
                Id = Guid.NewGuid(),
                Kind = TypeKind.Structural,
                Name = "Folder",
                Color = "#4285F4",
                CreatedAt = now,
                LastModifiedAt = now
            };
            ctx.Types.Add(folderType);
            await ctx.SaveChangesAsync();
            logger.LogInformation("Folder type created with ID: {TypeId}", folderType.Id);
        }

        // Create Root node if no nodes exist
        if (!await ctx.Nodes.AnyAsync())
        {
            var folderType = await ctx.Types.FirstAsync(t => t.Name == "Folder");
            logger.LogInformation("Creating Root node...");
            var rootNode = new NodeEntity
            {
                Id = Guid.NewGuid(),
                ParentId = null,
                PublicId = 1,
                Manifest = "",
                Caption = "Root",
                Description = "Root node of the hierarchy",
                Summary = "Root",
                TypeId = folderType.Id,
                WorkflowId = null,
                StateId = null,
                CreatedAt = now,
                LastModifiedAt = now
            };
            ctx.Nodes.Add(rootNode);
            await ctx.SaveChangesAsync();
            logger.LogInformation("Root node created with ID: {NodeId}, PublicId: 1", rootNode.Id);
        }

        logger.LogInformation("Seeding initial data complete.");
    }
}
