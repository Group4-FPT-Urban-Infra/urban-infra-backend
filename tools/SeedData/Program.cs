using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using UrbanInfraSystem.Infrastructure.Identity;
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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Seed in order respecting dependencies:
        // 1. Basic reference data (no dependencies)
        await new SeedAreas(db, logger).SeedAsync();
        await new SeedDepartments(db, logger).SeedAsync();
        await new SeedIssueTypes(db, logger).SeedAsync();
        await new SeedIssuePriorities(db, logger).SeedAsync();
        await new SeedIssueStatuses(db, logger).SeedAsync();

        // 2. Users and department members (depends on departments)
        await new SeedUsers(db, userManager, roleManager, logger).SeedAsync();

        // 3. Routing rules (depends on issue types, areas, departments)
        await new SeedRoutingRules(db, logger).SeedAsync();

        // 4. SLA policies (depends on issue types, priorities)
        await new SeedSlaPolicies(db, logger).SeedAsync();

        // 5. Escalation rules (depends on SLA policies)
        await new SeedEscalationRules(db, logger).SeedAsync();

        // 6. Issues (depends on users, issue types, priorities, statuses, areas)
        await new SeedIssues(db, logger).SeedAsync();

        // 7. Issue-related data: SLA tracking, upvotes, attachments (depends on issues, SLA policies)
        await new SeedIssueRelatedData(db, logger).SeedAsync();

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

    services.AddIdentity<ApplicationUser, ApplicationRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

    services.AddScoped<UserManager<ApplicationUser>>();
    services.AddScoped<RoleManager<ApplicationRole>>();

    return services.BuildServiceProvider();
}
