using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using UrbanInfraSystem.Infrastructure.Persistence;

namespace UrbanInfraSystem.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=UrbanInfraSystemDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.UseNetTopologySuite();
        });

        return new AppDbContext(optionsBuilder.Options);
    }
}
