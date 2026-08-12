using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using UrbanInfraSystem.Infrastructure.Persistence;
using UrbanInfraSystem.SeedData.Seeders;

var builder = RootCommand();

await builder.InvokeAsync(args);

static RootCommand RootCommand()
{
    var root = new RootCommand("Seed sample data for UrbanInfraSystem");

    var seedCmd = new Command("seed", "Seed all sample data");
    seedCmd.SetHandler(async () =>
    {
        var services = BuildServices();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting seed process...");

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await new SeedAreas(db, logger).SeedAsync();
        await new SeedDepartments(db, logger).SeedAsync();
        await new SeedIssueTypes(db, logger).SeedAsync();
        await new SeedIssuePriorities(db, logger).SeedAsync();
        await new SeedIssueStatuses(db, logger).SeedAsync();
        await new SeedDepartmentMembers(db, logger).SeedAsync();
        await new SeedIssues(db, logger).SeedAsync();

        logger.LogInformation("Seed completed successfully!");
    });

    root.AddCommand(seedCmd);
    return root;
}

static IServiceProvider BuildServices()
{
    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build();

    var connString = config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    var services = new ServiceCollection();
    services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connString, sqlOptions =>
            sqlOptions.UseNetTopologySuite()));

    return services.BuildServiceProvider();
}
