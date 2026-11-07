using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolManagement.Application.Auth;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Domain.Constants;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Infrastructure.Identity;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Auth;

public class AuthServiceTests
{
    private const string Password = "Correct-Password1";
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly PasswordHasher _passwordHasher = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = new string('k', 64),
            AccessTokenMinutes = 60,
            RememberMeDays = 7
        });

        _sut = new AuthService(
            _db,
            _passwordHasher,
            new JwtTokenService(jwtOptions, new FixedTimeProvider(Now)),
            NullLogger<AuthService>.Instance);

        _db.Roles.Add(new Role { Id = RoleIds.Teacher, Name = RoleNames.Teacher });
    }

    private async Task<User> AddUserAsync(string email = "teacher@school.test", bool isActive = true)
    {
        var user = new User
        {
            FullName = "משה כהן",
            Email = email,
            PasswordHash = _passwordHasher.Hash(Password),
            RoleId = RoleIds.Teacher,
            IsActive = isActive
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_token_and_user()
    {
        var user = await AddUserAsync();

        var result = await _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = Password });

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.User.Should().Be(new CurrentUserDto(user.Id, "משה כהן", user.Email, RoleNames.Teacher));
        result.ExpiresAt.Should().Be(Now.UtcDateTime.AddMinutes(60));
    }

    [Fact]
    public async Task Login_with_remember_me_returns_long_lived_token()
    {
        var user = await AddUserAsync();

        var result = await _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = Password, RememberMe = true });

        result.ExpiresAt.Should().Be(Now.UtcDateTime.AddDays(7));
    }

    [Fact]
    public async Task Login_ignores_email_case_and_surrounding_spaces()
    {
        await AddUserAsync("teacher@school.test");

        var act = () => _sut.LoginAsync(new LoginRequest { Email = "  Teacher@School.TEST ", Password = Password });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Login_with_wrong_password_fails()
    {
        var user = await AddUserAsync();

        var act = () => _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "wrong" });

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Login_with_unknown_email_fails_with_the_same_message_as_a_wrong_password()
    {
        var act = () => _sut.LoginAsync(new LoginRequest { Email = "nobody@school.test", Password = Password });

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Login_for_inactive_user_fails()
    {
        var user = await AddUserAsync(isActive: false);

        var act = () => _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = Password });

        await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage("This account is disabled.");
    }

    [Fact]
    public async Task GetCurrentUser_for_disabled_user_fails()
    {
        var user = await AddUserAsync(isActive: false);

        var act = () => _sut.GetCurrentUserAsync(user.Id);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }
}
