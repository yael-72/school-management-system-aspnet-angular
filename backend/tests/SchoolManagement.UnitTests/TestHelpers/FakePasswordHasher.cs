using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.UnitTests.TestHelpers;

/// <summary>A readable stand-in for the real PBKDF2 hasher, so tests can check what was hashed.</summary>
public class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string passwordHash, string password) => passwordHash == Hash(password);
}
