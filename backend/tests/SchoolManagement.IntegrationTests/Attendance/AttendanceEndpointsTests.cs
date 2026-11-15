using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Domain.Enums;
using SchoolManagement.IntegrationTests.Infrastructure;

namespace SchoolManagement.IntegrationTests.Attendance;

public class AttendanceEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // The API sends enums as names ("Present"), so the test client must read them the same way.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private int TeacherClassId => factory.ClassId(ApiFactory.TeacherClassName);

    private int StudentId(string firstName) =>
        factory.QueryDatabase(db => db.Students.Single(s => s.FirstName == firstName).Id);

    private object SaveBody(DateOnly date, string status) => new
    {
        classId = TeacherClassId,
        date,
        records = new[]
        {
            new { studentId = StudentId("יוסף"), status, note = (string?)null },
            new { studentId = StudentId("דוד"), status = "Present", note = (string?)"הגיע מאוחר מהטיול" }
        }
    };

    [Fact]
    public async Task Teacher_saves_attendance_and_saving_again_updates_it()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var date = Today.AddDays(-1);

        (await client.PutAsJsonAsync("/api/attendance", SaveBody(date, "Absent"))).EnsureSuccessStatusCode();
        var response = await client.PutAsJsonAsync("/api/attendance", SaveBody(date, "Late"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var register = await response.Content.ReadFromJsonAsync<ClassAttendanceDto>(Json);
        register!.Students.Single(s => s.FirstName == "יוסף").Status.Should().Be(AttendanceStatus.Late);
        factory.QueryDatabase(db => db.Attendance.Count(a => a.Date == date)).Should().Be(2);
    }

    [Fact]
    public async Task Register_returns_statuses_as_names()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        var date = Today.AddDays(-2);
        (await client.PutAsJsonAsync("/api/attendance", SaveBody(date, "Excused"))).EnsureSuccessStatusCode();

        var json = await client.GetStringAsync($"/api/attendance?classId={TeacherClassId}&date={date:yyyy-MM-dd}");

        json.Should().Contain("\"status\":\"Excused\"");
    }

    [Fact]
    public async Task Future_date_is_rejected_with_problem_details()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PutAsJsonAsync("/api/attendance", SaveBody(Today.AddDays(2), "Present"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("future date");
    }

    [Fact]
    public async Task Unknown_status_is_a_validation_error()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PutAsJsonAsync("/api/attendance", SaveBody(Today, "Sleeping"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Missing_date_is_a_validation_error()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.GetAsync($"/api/attendance?classId={TeacherClassId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Teacher_gets_403_for_another_class()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var otherClassId = factory.ClassId(ApiFactory.OtherClassName);

        var response = await client.GetAsync($"/api/attendance?classId={otherClassId}&date={Today:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Report_returns_a_row_per_active_student()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var from = Today.AddDays(-30);

        var report = await client.GetFromJsonAsync<AttendanceReportDto>(
            $"/api/attendance/report?classId={TeacherClassId}&from={from:yyyy-MM-dd}&to={Today:yyyy-MM-dd}", Json);

        report!.Students.Select(s => s.FirstName).Should().BeEquivalentTo("יוסף", "דוד");
    }

    [Fact]
    public async Task Requests_without_a_token_get_401()
    {
        var response = await factory.CreateClient().GetAsync($"/api/attendance/report?classId=1&from=2026-10-01&to=2026-10-31");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
