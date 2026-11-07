using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Entities;
using AspNetPasswordHasher = Microsoft.AspNetCore.Identity.PasswordHasher<SchoolManagement.Domain.Entities.User>;
using PasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace SchoolManagement.Infrastructure.Identity;

/// <summary>
/// Wraps ASP.NET Core Identity's PasswordHasher (PBKDF2 with a random salt per password),
/// a well-tested implementation, instead of writing our own hashing code.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private static readonly User UnusedUser = new();
    private readonly AspNetPasswordHasher _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(UnusedUser, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(UnusedUser, passwordHash, password) != PasswordVerificationResult.Failed;
}
