using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Students;
using SchoolManagement.IntegrationTests.Infrastructure;

namespace SchoolManagement.IntegrationTests.Students;

public class StudentsEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private int StudentId(string firstName) =>
        factory.QueryDatabase(db => db.Students.Single(s => s.FirstName == firstName).Id);

    [Fact]
    public async Task Search_and_paging_parameters_are_applied()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var page = await client.GetFromJsonAsync<PagedResult<StudentListItemDto>>(
            "/api/students?search=כהן&page=1&pageSize=10&sortBy=lastName&sortDir=desc");

        page!.Items.Select(s => s.FirstName).Should().Equal("דוד");
        page.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Teacher_gets_403_for_a_student_in_another_class()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.GetAsync($"/api/students/{StudentId("משה")}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_can_read_the_summary_of_her_student()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var summary = await client.GetFromJsonAsync<StudentSummaryDto>($"/api/students/{StudentId("יוסף")}/summary");

        summary!.Student.ClassName.Should().Be(ApiFactory.TeacherClassName);
    }

    [Fact]
    public async Task Teacher_cannot_create_students()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PostAsJsonAsync("/api/students", new SaveStudentRequest
        {
            FirstName = "אליהו",
            LastName = "שמש",
            ClassId = factory.ClassId(ApiFactory.TeacherClassName)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_creates_and_then_soft_deletes_a_student()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var createResponse = await client.PostAsJsonAsync("/api/students", new SaveStudentRequest
        {
            FirstName = "אליהו",
            LastName = "שמש",
            ClassId = factory.ClassId(ApiFactory.OtherClassName),
            BirthDate = new DateOnly(2013, 3, 14)
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<StudentDto>();

        var deleteResponse = await client.DeleteAsync($"/api/students/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/students/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        factory.QueryDatabase(db => db.Students.Single(s => s.Id == created.Id).IsActive).Should().BeFalse();
    }
}
