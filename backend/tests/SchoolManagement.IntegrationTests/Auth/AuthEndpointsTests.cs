using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Application.Auth;
using SchoolManagement.Domain.Constants;
using SchoolManagement.IntegrationTests.Infrastructure;

namespace SchoolManagement.IntegrationTests.Auth;

public class AuthEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });

    [Fact]
    public async Task Login_with_valid_credentials_returns_token()
    {
        var response = await LoginAsync(ApiFactory.AdminEmail, ApiFactory.Password);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.User.Role.Should().Be(RoleNames.Admin);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_problem_details()
    {
        var response = await LoginAsync(ApiFactory.AdminEmail, "wrong-password");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Detail.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Login_for_inactive_user_returns_401()
    {
        var response = await LoginAsync(ApiFactory.InactiveEmail, ApiFactory.Password);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_with_invalid_input_returns_400_with_field_errors()
    {
        var response = await LoginAsync("not-an-email", "");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKeys("Email", "Password");
    }

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_invalid_token_returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_with_token_returns_the_logged_in_user()
    {
        var login = await (await LoginAsync(ApiFactory.TeacherEmail, ApiFactory.Password))
            .Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        me!.Email.Should().Be(ApiFactory.TeacherEmail);
        me.Role.Should().Be(RoleNames.Teacher);
    }
}
