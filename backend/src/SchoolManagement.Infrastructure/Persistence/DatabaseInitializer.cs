using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Infrastructure.Persistence.Seed;

namespace SchoolManagement.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending EF Core migrations and fills an empty database with demo data.
    /// Meant for development and the demo environment; a real production deployment would run
    /// migrations as a separate deployment step.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync(cancellationToken);

        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
