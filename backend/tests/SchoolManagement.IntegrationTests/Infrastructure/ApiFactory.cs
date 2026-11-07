using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Constants;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Infrastructure.Persistence;

namespace SchoolManagement.IntegrationTests.Infrastructure;

/// <summary>
/// Starts the real API in memory (real routing, auth, validation and error handling),
/// with an in-memory database instead of SQL Server and a few known test users.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "Test-Password1";
    public const string AdminEmail = "admin@test.local";
    public const string TeacherEmail = "teacher@test.local";
    public const string InactiveEmail = "inactive@test.local";

    private readonly string _databaseName = $"api-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting (not ConfigureAppConfiguration) because Program.cs reads these values
        // while it is still registering services.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "not-used-in-tests");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-that-is-long-enough-0123456789");

        builder.ConfigureTestServices(services =>
        {
            // Replace the SQL Server registration with an in-memory database.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        SeedTestData(host.Services);
        return host;
    }

    private static void SeedTestData(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        db.Roles.AddRange(
            new Role { Id = RoleIds.Admin, Name = RoleNames.Admin },
            new Role { Id = RoleIds.Teacher, Name = RoleNames.Teacher });

        User NewUser(string name, string email, int roleId, bool isActive = true) => new()
        {
            FullName = name,
            Email = email,
            PasswordHash = hasher.Hash(Password),
            RoleId = roleId,
            IsActive = isActive
        };

        db.Users.AddRange(
            NewUser("מנהל בדיקות", AdminEmail, RoleIds.Admin),
            NewUser("מורה בדיקות", TeacherEmail, RoleIds.Teacher),
            NewUser("משתמש מושבת", InactiveEmail, RoleIds.Teacher, isActive: false));

        db.SaveChanges();
    }
}
