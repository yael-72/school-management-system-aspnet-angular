using FluentAssertions;
using SchoolManagement.Infrastructure.Identity;

namespace SchoolManagement.UnitTests.Auth;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_never_contains_the_plain_password_and_uses_a_random_salt()
    {
        var first = _sut.Hash("Demo123!");
        var second = _sut.Hash("Demo123!");

        first.Should().NotContain("Demo123!");
        first.Should().NotBe(second, "each hash has its own random salt");
    }

    [Fact]
    public void Verify_accepts_only_the_original_password()
    {
        var hash = _sut.Hash("Demo123!");

        _sut.Verify(hash, "Demo123!").Should().BeTrue();
        _sut.Verify(hash, "demo123!").Should().BeFalse();
    }
}
