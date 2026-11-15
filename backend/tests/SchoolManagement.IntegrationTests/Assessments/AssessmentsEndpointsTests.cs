using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using SchoolManagement.Application.Assessments;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Subjects;
using SchoolManagement.IntegrationTests.Infrastructure;

namespace SchoolManagement.IntegrationTests.Assessments;

public class AssessmentsEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // The API sends enums as names ("Exam"), so the test client must read them the same way.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private int StudentId(string firstName) =>
        factory.QueryDatabase(db => db.Students.Single(s => s.FirstName == firstName).Id);

    private object NewAssessment(string subject = "מתמטיקה", string className = ApiFactory.TeacherClassName, int maxScore = 100) => new
    {
        title = "מבחן בשברים",
        type = "Exam",
        classId = factory.ClassId(className),
        subjectId = factory.SubjectId(subject),
        date = Today.AddDays(-1),
        maxScore
    };

    private async Task<AssessmentDto> CreateAsync(HttpClient client, int maxScore = 100)
    {
        var response = await client.PostAsJsonAsync("/api/assessments", NewAssessment(maxScore: maxScore));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<AssessmentDto>(Json))!;
    }

    [Fact]
    public async Task Teacher_creates_an_assessment_and_grades_her_class()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var assessment = await CreateAsync(client, maxScore: 20);

        var response = await client.PutAsJsonAsync($"/api/assessments/{assessment.Id}/scores", new
        {
            scores = new object[]
            {
                new { studentId = StudentId("יוסף"), score = 18 },
                new { studentId = StudentId("דוד"), score = (decimal?)null, comment = "חולה ביום המבחן" }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sheet = await response.Content.ReadFromJsonAsync<AssessmentScoresDto>(Json);
        sheet!.GradedCount.Should().Be(1);
        sheet.AverageScore.Should().Be(90);
        sheet.Students.Single(s => s.FirstName == "דוד").Comment.Should().Be("חולה ביום המבחן");
    }

    [Fact]
    public async Task Score_above_the_max_score_is_rejected_with_problem_details()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var assessment = await CreateAsync(client, maxScore: 20);

        var response = await client.PutAsJsonAsync($"/api/assessments/{assessment.Id}/scores", new
        {
            scores = new[] { new { studentId = StudentId("יוסף"), score = 21 } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("max score (20)");
    }

    [Fact]
    public async Task Negative_score_is_a_validation_error()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var assessment = await CreateAsync(client);

        var response = await client.PutAsJsonAsync($"/api/assessments/{assessment.Id}/scores", new
        {
            scores = new[] { new { studentId = StudentId("יוסף"), score = -5 } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Teacher_gets_403_for_a_subject_she_does_not_teach()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PostAsJsonAsync("/api/assessments", NewAssessment(subject: "אנגלית"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_gets_403_for_another_class()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.GetAsync($"/api/assessments?classId={factory.ClassId(ApiFactory.OtherClassName)}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_returns_a_page_with_the_edit_permission_of_each_row()
    {
        var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        await CreateAsync(admin);

        var teacher = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var page = await teacher.GetFromJsonAsync<PagedResult<AssessmentListItemDto>>(
            $"/api/assessments?classId={factory.ClassId(ApiFactory.TeacherClassName)}&sortBy=date&sortDir=desc", Json);

        page!.Items.Should().NotBeEmpty().And.OnlyContain(a => a.CanEdit && a.ClassName == ApiFactory.TeacherClassName);
    }

    [Fact]
    public async Task Missing_title_is_a_validation_error()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var response = await client.PostAsJsonAsync("/api/assessments", new
        {
            title = "",
            type = "Exam",
            classId = factory.ClassId(ApiFactory.TeacherClassName),
            subjectId = factory.SubjectId("מתמטיקה"),
            date = Today
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Title is required.");
    }

    [Fact]
    public async Task Deleted_assessment_returns_404_afterwards()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var assessment = await CreateAsync(client);

        (await client.DeleteAsync($"/api/assessments/{assessment.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await client.GetAsync($"/api/assessments/{assessment.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Subjects_are_listed_alphabetically_for_any_user()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var subjects = await client.GetFromJsonAsync<List<SubjectDto>>("/api/subjects", Json);

        subjects!.Select(s => s.Name).Should().Equal("אנגלית", "מתמטיקה");
    }

    [Fact]
    public async Task Requests_without_a_token_get_401()
    {
        var response = await factory.CreateClient().GetAsync("/api/assessments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
