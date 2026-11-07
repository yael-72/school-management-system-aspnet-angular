using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Seed;

namespace SchoolManagement.Infrastructure;

/// <summary>
/// Registers everything the Infrastructure layer provides, so Program.cs only needs one call:
/// builder.Services.AddInfrastructure(builder.Configuration).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing. Set it in appsettings, user-secrets or the " +
                "ConnectionStrings__DefaultConnection environment variable.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        // PBKDF2 password hashing from ASP.NET Core Identity, without pulling in the full Identity system.
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<DbSeeder>();

        return services;
    }
}
