using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Students;
using SchoolManagement.IntegrationTests.Infrastructure;

namespace SchoolManagement.IntegrationTests.Classes;

public class ClassesEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static SaveClassRequest NewClass(string name) => new() { Name = name, GradeLevel = 9, SchoolYear = "2026-2027" };

    [Fact]
    public async Task Admin_gets_all_classes_as_a_paged_result()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var page = await client.GetFromJsonAsync<PagedResult<ClassSummaryDto>>("/api/classes?pageSize=50");

        page!.Items.Select(c => c.Name).Should().Contain([ApiFactory.TeacherClassName, ApiFactory.OtherClassName]);
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task Teacher_only_gets_the_classes_she_teaches_in()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var page = await client.GetFromJsonAsync<PagedResult<ClassSummaryDto>>("/api/classes");

        page!.Items.Select(c => c.Name).Should().Equal(ApiFactory.TeacherClassName);
        page.Items.Single().StudentCount.Should().Be(2);
    }

    [Fact]
    public async Task Teacher_gets_403_problem_details_for_another_class()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.GetAsync($"/api/classes/{factory.ClassId(ApiFactory.OtherClassName)}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Class_students_endpoint_returns_the_students_of_the_class()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var page = await client.GetFromJsonAsync<PagedResult<StudentListItemDto>>(
            $"/api/classes/{factory.ClassId(ApiFactory.TeacherClassName)}/students?sortBy=firstName");

        page!.Items.Select(s => s.FirstName).Should().Equal("דוד", "יוסף");
    }

    [Fact]
    public async Task Teacher_cannot_create_a_class()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PostAsJsonAsync("/api/classes", NewClass("ט'5"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_creates_updates_and_deletes_an_empty_class()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var createResponse = await client.PostAsJsonAsync("/api/classes", NewClass("ט'2"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull();
        var created = await createResponse.Content.ReadFromJsonAsync<ClassDetailsDto>();

        var updateResponse = await client.PutAsJsonAsync($"/api/classes/{created!.Id}", NewClass("ט'3"));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await updateResponse.Content.ReadFromJsonAsync<ClassDetailsDto>())!.Name.Should().Be("ט'3");

        var deleteResponse = await client.DeleteAsync($"/api/classes/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/classes/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deleting_a_class_with_active_students_returns_400_with_a_clear_message()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var response = await client.DeleteAsync($"/api/classes/{factory.ClassId(ApiFactory.OtherClassName)}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Detail.Should().Contain("active students");
    }

    [Fact]
    public async Task Creating_an_invalid_class_returns_field_errors()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var response = await client.PostAsJsonAsync("/api/classes", new SaveClassRequest { Name = "", GradeLevel = 13, SchoolYear = "2026" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKeys("Name", "GradeLevel", "SchoolYear");
    }

    [Fact]
    public async Task List_rejects_a_page_size_above_the_limit()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.AdminEmail);

        var response = await client.GetAsync("/api/classes?pageSize=1000");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
