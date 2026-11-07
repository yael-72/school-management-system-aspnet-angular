using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SchoolManagement.Domain.Constants;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Infrastructure.Persistence;

namespace SchoolManagement.UnitTests.Persistence;

/// <summary>
/// Checks that the EF Core model enforces the business rules that belong in the database
/// (uniqueness, delete behavior). Building the model does not open a database connection,
/// so these tests are fast and need no SQL Server.
/// </summary>
public class AppDbContextModelTests
{
    // The design-time model keeps everything (including seed data); the runtime model is trimmed for speed.
    private static readonly IModel Model = CreateContext().GetService<IDesignTimeModel>().Model;

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .Options);

    [Theory]
    [InlineData(typeof(Score), new[] { nameof(Score.AssessmentId), nameof(Score.StudentId) })]
    [InlineData(typeof(AttendanceRecord), new[] { nameof(AttendanceRecord.StudentId), nameof(AttendanceRecord.Date) })]
    [InlineData(typeof(TeacherAssignment), new[] { nameof(TeacherAssignment.TeacherId), nameof(TeacherAssignment.ClassId), nameof(TeacherAssignment.SubjectId) })]
    [InlineData(typeof(User), new[] { nameof(User.Email) })]
    public void Entity_has_unique_index_on_expected_columns(Type entityType, string[] columns)
    {
        var entity = Model.FindEntityType(entityType)!;

        entity.GetIndexes()
            .Should().Contain(index =>
                index.IsUnique && index.Properties.Select(p => p.Name).SequenceEqual(columns));
    }

    [Fact]
    public void Deleting_a_class_with_students_is_restricted()
    {
        var foreignKey = Model.FindEntityType(typeof(Student))!
            .GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(SchoolClass));

        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    [Fact]
    public void Roles_are_seeded_with_admin_and_teacher()
    {
        var roles = Model.FindEntityType(typeof(Role))!.GetSeedData().ToList();

        roles.Should().HaveCount(2);
        roles.Should().ContainSingle(r => (int)r[nameof(Role.Id)]! == RoleIds.Admin && (string)r[nameof(Role.Name)]! == RoleNames.Admin);
        roles.Should().ContainSingle(r => (int)r[nameof(Role.Id)]! == RoleIds.Teacher && (string)r[nameof(Role.Name)]! == RoleNames.Teacher);
    }

    [Fact]
    public void Model_has_no_pending_changes_compared_to_the_migrations()
    {
        // Fails when someone changes an entity or configuration but forgets to add a migration.
        using var context = CreateContext();

        context.Database.HasPendingModelChanges().Should().BeFalse();
    }
}
