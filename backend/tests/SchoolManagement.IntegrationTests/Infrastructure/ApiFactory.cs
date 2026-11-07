using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SchoolManagement.Application.Auth;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Constants;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Infrastructure.Persistence;

namespace SchoolManagement.IntegrationTests.Infrastructure;

/// <summary>
/// Starts the real API in memory (real routing, auth, validation and error handling),
/// with an in-memory database instead of SQL Server and a small known school:
/// an admin, a teacher who teaches only in <see cref="TeacherClassName"/>, and a disabled user.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "Test-Password1";
    public const string AdminEmail = "admin@test.local";
    public const string TeacherEmail = "teacher@test.local";
    public const string InactiveEmail = "inactive@test.local";

    /// <summary>The class the test teacher teaches in. She has no access to <see cref="OtherClassName"/>.</summary>
    public const string TeacherClassName = "ז'1";
    public const string OtherClassName = "ח'1";

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

        var teacher = NewUser("מורה בדיקות", TeacherEmail, RoleIds.Teacher);
        db.Users.AddRange(
            NewUser("מנהל בדיקות", AdminEmail, RoleIds.Admin),
            teacher,
            NewUser("משתמש מושבת", InactiveEmail, RoleIds.Teacher, isActive: false));

        var teacherClass = new SchoolClass { Name = TeacherClassName, GradeLevel = 7, SchoolYear = "2026-2027" };
        var otherClass = new SchoolClass { Name = OtherClassName, GradeLevel = 8, SchoolYear = "2026-2027" };
        var math = new Subject { Name = "מתמטיקה" };
        db.TeacherAssignments.Add(new TeacherAssignment { Teacher = teacher, Class = teacherClass, Subject = math });

        db.Students.AddRange(
            new Student { FirstName = "יוסף", LastName = "לוי", Class = teacherClass },
            new Student { FirstName = "דוד", LastName = "כהן", Class = teacherClass },
            new Student { FirstName = "משה", LastName = "פרץ", Class = otherClass });

        db.SaveChanges();
    }

    /// <summary>An HTTP client that is already logged in as the given user.</summary>
    public async Task<HttpClient> CreateClientForAsync(string email)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = Password });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }

    /// <summary>Reads data straight from the test database (to find ids or check what an endpoint saved).</summary>
    public T QueryDatabase<T>(Func<AppDbContext, T> query)
    {
        using var scope = Services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public int ClassId(string name) => QueryDatabase(db => db.Classes.Single(c => c.Name == name).Id);
}
