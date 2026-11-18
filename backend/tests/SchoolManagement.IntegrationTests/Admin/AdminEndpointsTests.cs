using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SchoolManagement.Application.Auth;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Subjects;
using SchoolManagement.Application.TeacherAssignments;
using SchoolManagement.Application.Users;
using SchoolManagement.Domain.Constants;
using SchoolManagement.IntegrationTests.Infrastructure;

namespace SchoolManagement.IntegrationTests.Admin;

/// <summary>User, subject and teacher-assignment management – the admin screens.</summary>
public class AdminEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("GET", "/api/users")]
    [InlineData("GET", "/api/roles")]
    [InlineData("GET", "/api/teacher-assignments")]
    [InlineData("POST", "/api/subjects")]
    [InlineData("POST", "/api/users")]
    [InlineData("PATCH", "/api/users/1/status")]
    public async Task Teachers_get_403_on_admin_endpoints(string method, string url)
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "GET" ? null : JsonContent.Create(new { })
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_user_created_by_the_admin_can_log_in_and_is_blocked_after_deactivation()
    {
        var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        var request = new CreateUserRequest
        {
            FullName = "מורה חדש", Email = "New.Teacher@test.local", RoleId = RoleIds.Teacher, Password = "Welcome123"
        };

        var created = await (await admin.PostAsJsonAsync("/api/users", request)).Content.ReadFromJsonAsync<UserDto>();
        var login = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "new.teacher@test.local", Password = "Welcome123" });
        (await admin.PatchAsJsonAsync($"/api/users/{created!.Id}/status", new { isActive = false })).EnsureSuccessStatusCode();
        var blocked = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "new.teacher@test.local", Password = "Welcome123" });

        created.Role.Should().Be(RoleNames.Teacher);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        blocked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_weak_password_is_a_validation_error()
    {
        var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var response = await admin.PostAsJsonAsync("/api/users", new CreateUserRequest
        {
            FullName = "מורה", Email = "weak@test.local", RoleId = RoleIds.Teacher, Password = "12345678"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("a letter and a digit");
    }

    [Fact]
    public async Task Admin_cannot_deactivate_herself()
    {
        var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        var adminId = factory.QueryDatabase(db => db.Users.Single(u => u.Email == ApiFactory.AdminEmail).Id);

        var response = await admin.PatchAsJsonAsync($"/api/users/{adminId}/status", new { isActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Roles_are_listed_for_the_user_form()
    {
        var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var roles = await admin.GetFromJsonAsync<List<RoleDto>>("/api/roles");

        roles!.Select(r => r.Name).Should().Equal(RoleNames.Admin, RoleNames.Teacher);
    }

    [Fact]
    public async Task Admin_manages_subjects_and_teachers_can_still_read_them()
    {
        var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var created = await (await admin.PostAsJsonAsync("/api/subjects", new { name = "ספרות" }))
            .Content.ReadFromJsonAsync<SubjectDto>();
        (await admin.PutAsJsonAsync($"/api/subjects/{created!.Id}", new { name = "ספרות עברית" })).EnsureSuccessStatusCode();

        var teacher = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        (await teacher.GetFromJsonAsync<List<SubjectDto>>("/api/subjects")).Should().Contain(s => s.Name == "ספרות עברית");

        (await admin.DeleteAsync($"/api/subjects/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Assigning_a_teacher_to_a_class_gives_her_access_and_removing_it_takes_it_away()
    {
        var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        var teacher = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var teacherId = factory.QueryDatabase(db => db.Users.Single(u => u.Email == ApiFactory.TeacherEmail).Id);
        var otherClassId = factory.ClassId(ApiFactory.OtherClassName);
        var english = factory.SubjectId("אנגלית");
        var classUrl = $"/api/classes/{otherClassId}";

        (await teacher.GetAsync(classUrl)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var response = await admin.PostAsJsonAsync("/api/teacher-assignments",
            new CreateTeacherAssignmentRequest { TeacherId = teacherId, ClassId = otherClassId, SubjectId = english });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var assignment = await response.Content.ReadFromJsonAsync<TeacherAssignmentDto>();
        (await teacher.GetAsync(classUrl)).StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await admin.GetFromJsonAsync<PagedResult<TeacherAssignmentDto>>($"/api/teacher-assignments?teacherId={teacherId}");
        page!.Items.Should().Contain(a => a.Id == assignment!.Id && a.SubjectName == "אנגלית");

        (await admin.DeleteAsync($"/api/teacher-assignments/{assignment!.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await teacher.GetAsync(classUrl)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
