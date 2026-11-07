using Microsoft.EntityFrameworkCore;
using SchoolManagement.Infrastructure.Persistence;

namespace SchoolManagement.UnitTests.TestHelpers;

public static class TestDbContextFactory
{
    /// <summary>
    /// A fresh in-memory database per test, so tests never affect each other.
    /// Fine for testing service logic; database constraints are covered by AppDbContextModelTests.
    /// </summary>
    public static AppDbContext Create() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
