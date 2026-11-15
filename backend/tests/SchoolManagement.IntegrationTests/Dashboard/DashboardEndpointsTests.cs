using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using SchoolManagement.Application.Dashboard;
using SchoolManagement.Application.Events;
using SchoolManagement.Application.Tasks;
using SchoolManagement.IntegrationTests.Infrastructure;

namespace SchoolManagement.IntegrationTests.Dashboard;

/// <summary>Dashboard numbers, the to-do list and the calendar – the endpoints behind the dashboard page.</summary>
public class DashboardEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Theory]
    [InlineData("/api/dashboard/stats")]
    [InlineData("/api/dashboard/grades-trend?months=6")]
    [InlineData("/api/dashboard/attendance-weekly")]
    [InlineData("/api/dashboard/grade-distribution")]
    [InlineData("/api/dashboard/top-students?count=5")]
    [InlineData("/api/dashboard/recent-activity")]
    public async Task Every_dashboard_endpoint_answers_a_teacher(string url)
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Teacher_stats_are_scoped_to_her_class()
    {
        var teacher = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var teacherStats = await teacher.GetFromJsonAsync<DashboardStatsDto>("/api/dashboard/stats", Json);
        var adminStats = await admin.GetFromJsonAsync<DashboardStatsDto>("/api/dashboard/stats", Json);

        teacherStats!.ClassCount.Should().Be(1);
        teacherStats.StudentCount.Should().Be(2);
        adminStats!.StudentCount.Should().Be(3);
    }

    [Fact]
    public async Task Too_many_months_is_a_validation_error()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var response = await client.GetAsync("/api/dashboard/grades-trend?months=24");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task To_do_list_supports_add_toggle_and_delete()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var created = await (await client.PostAsJsonAsync("/api/tasks", new { title = "לבדוק מבחנים", dueDate = Today }))
            .Content.ReadFromJsonAsync<TodoTaskDto>(Json);
        var toggled = await (await client.PatchAsync($"/api/tasks/{created!.Id}/toggle", null))
            .Content.ReadFromJsonAsync<TodoTaskDto>(Json);
        var deleted = await client.DeleteAsync($"/api/tasks/{created.Id}");

        toggled!.IsDone.Should().BeTrue();
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<List<TodoTaskDto>>("/api/tasks", Json)).Should().NotContain(t => t.Id == created.Id);
    }

    [Fact]
    public async Task Another_users_task_returns_404()
    {
        var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        var task = await (await admin.PostAsJsonAsync("/api/tasks", new { title = "פרטי" }))
            .Content.ReadFromJsonAsync<TodoTaskDto>(Json);

        var teacher = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var response = await teacher.DeleteAsync($"/api/tasks/{task!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Teacher_cannot_add_a_school_wide_event()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PostAsJsonAsync("/api/events", new { title = "יום ספורט", startDate = Today });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_adds_an_event_that_teachers_see_in_the_calendar()
    {
        var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        (await admin.PostAsJsonAsync("/api/events", new { title = "אסיפת הורים", startDate = Today.AddDays(2) }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var teacher = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var events = await teacher.GetFromJsonAsync<List<EventDto>>(
            $"/api/events?from={Today:yyyy-MM-dd}&to={Today.AddDays(30):yyyy-MM-dd}", Json);

        events.Should().Contain(e => e.Title == "אסיפת הורים" && !e.CanEdit);
    }
}
