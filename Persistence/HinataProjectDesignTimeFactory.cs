using HinataProject.Persistence.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HinataProject.Persistence;

public class HinataProjectDesignTimeFactory : IDesignTimeDbContextFactory<HinataProjectDataContext>
{
    public HinataProjectDataContext CreateDbContext(string[] args)
    {
        //Args is empty for delete/list operation, even when filled. Looks like bugg in ef core migration tools (2021-11-01) 
        var connectionString = args.Length > 0
            ? args[0]
            : Environment.GetEnvironmentVariable("HinataProject_ConnectionStrings__DbContext");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("DesignTimeFactory connection string is not set.");
            
        var builder = new DbContextOptionsBuilder<HinataProjectDataContext>();
        builder.ConfigureBuilder(connectionString);
        return new HinataProjectDataContext(builder.Options, TimeProvider.System);
    }
}