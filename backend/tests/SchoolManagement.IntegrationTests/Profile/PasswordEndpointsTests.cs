using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SchoolManagement.Application.Auth;
using SchoolManagement.IntegrationTests.Infrastructure;

namespace SchoolManagement.IntegrationTests.Profile;

/// <summary>
/// Changing your own password and an admin reset. Each test gets its own factory (a fresh database),
/// because changing a password would break the logins of the other tests in a shared one.
/// </summary>
public class PasswordEndpointsTests
{
    private static async Task<HttpStatusCode> LoginStatusAsync(ApiFactory factory, string email, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        return response.StatusCode;
    }

    [Fact]
    public async Task A_user_changes_her_password_and_logs_in_with_the_new_one()
    {
        using var factory = new ApiFactory();
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PutAsJsonAsync("/api/users/me/password",
            new { currentPassword = ApiFactory.Password, newPassword = "BrandNew123" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await LoginStatusAsync(factory, ApiFactory.TeacherEmail, "BrandNew123")).Should().Be(HttpStatusCode.OK);
        (await LoginStatusAsync(factory, ApiFactory.TeacherEmail, ApiFactory.Password)).Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_wrong_current_password_returns_400_and_keeps_the_user_logged_in()
    {
        using var factory = new ApiFactory();
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PutAsJsonAsync("/api/users/me/password",
            new { currentPassword = "NotMyPassword1", newPassword = "BrandNew123" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("current password is incorrect");
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_weak_new_password_is_a_validation_error()
    {
        using var factory = new ApiFactory();
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PutAsJsonAsync("/api/users/me/password",
            new { currentPassword = ApiFactory.Password, newPassword = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("a letter and a digit");
    }

    [Fact]
    public async Task Admin_resets_a_teachers_password_and_a_teacher_cannot()
    {
        using var factory = new ApiFactory();
        var teacherId = factory.QueryDatabase(db => db.Users.Single(u => u.Email == ApiFactory.TeacherEmail).Id);
        var adminId = factory.QueryDatabase(db => db.Users.Single(u => u.Email == ApiFactory.AdminEmail).Id);

        var teacher = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        (await teacher.PutAsJsonAsync($"/api/users/{adminId}/password", new { newPassword = "Hacked1234" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        (await admin.PutAsJsonAsync($"/api/users/{teacherId}/password", new { newPassword = "Reset12345" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await LoginStatusAsync(factory, ApiFactory.TeacherEmail, "Reset12345")).Should().Be(HttpStatusCode.OK);
    }
}
