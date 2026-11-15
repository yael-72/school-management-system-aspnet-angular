using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using SchoolManagement.Application.Files;
using SchoolManagement.Application.Profile;
using SchoolManagement.IntegrationTests.Infrastructure;

namespace SchoolManagement.IntegrationTests.Profile;

/// <summary>The profile page: own details, notification settings and private files.</summary>
public class ProfileEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static MultipartFormDataContent FileForm(string fileName, byte[] content)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    [Fact]
    public async Task Teacher_reads_and_updates_her_own_profile()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "מורה מעודכן", phone = "050-1234567" });
        var profile = await client.GetFromJsonAsync<ProfileDto>("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        profile!.FullName.Should().Be("מורה מעודכן");
        profile.Email.Should().Be(ApiFactory.TeacherEmail);
        profile.ClassCount.Should().Be(1);
    }

    [Fact]
    public async Task Invalid_phone_is_a_validation_error()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PutAsJsonAsync("/api/users/me", new { fullName = "מורה", phone = "abc" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Notification_settings_are_saved()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        var settings = new NotificationSettingsDto { RemindBeforeExam = false, AlertOnLowAttendance = true, WeeklySummaryEmail = true };

        (await client.PutAsJsonAsync("/api/users/me/notification-settings", settings)).EnsureSuccessStatusCode();

        (await client.GetFromJsonAsync<NotificationSettingsDto>("/api/users/me/notification-settings"))
            .Should().Be(settings);
    }

    [Fact]
    public async Task Uploaded_file_is_listed_downloaded_as_an_attachment_and_deleted()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);
        var content = Encoding.UTF8.GetBytes("מערכת שעות");

        var upload = await client.PostAsync("/api/files", FileForm("מערכת שעות.txt", content));
        upload.StatusCode.Should().Be(HttpStatusCode.Created);
        var file = await upload.Content.ReadFromJsonAsync<UploadedFileDto>();

        var overview = await client.GetFromJsonAsync<FilesOverviewDto>("/api/files");
        overview!.Files.Should().ContainSingle(f => f.Id == file!.Id);
        overview.UsedBytes.Should().Be(content.Length);

        var download = await client.GetAsync($"/api/files/{file!.Id}/download");
        download.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        download.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        (await download.Content.ReadAsByteArrayAsync()).Should().Equal(content);

        (await client.DeleteAsync($"/api/files/{file.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/files/{file.Id}/download")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Disallowed_file_type_is_rejected()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PostAsync("/api/files", FileForm("virus.exe", [1, 2, 3]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("file type is not allowed");
    }

    /// <remarks>
    /// The multipart form limit stops the upload before it reaches the service. (Kestrel may answer 413
    /// a little earlier through the request size limit; the in-memory test server does not enforce that one.)
    /// </remarks>
    [Fact]
    public async Task File_larger_than_the_upload_limit_is_rejected_while_reading_the_form()
    {
        var client = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        var response = await client.PostAsync("/api/files", FileForm("big.pdf", new byte[7 * 1024 * 1024]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("limit");
    }

    [Fact]
    public async Task Another_users_file_returns_404()
    {
        var admin = await factory.CreateClientForAsync(ApiFactory.AdminEmail);
        var file = await (await admin.PostAsync("/api/files", FileForm("admin.pdf", [1])))
            .Content.ReadFromJsonAsync<UploadedFileDto>();

        var teacher = await factory.CreateClientForAsync(ApiFactory.TeacherEmail);

        (await teacher.GetAsync($"/api/files/{file!.Id}/download")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
