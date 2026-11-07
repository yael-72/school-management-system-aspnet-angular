using FluentAssertions;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Security;

public class ClassAccessServiceTests
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly SchoolBuilder _school;

    public ClassAccessServiceTests() => _school = new SchoolBuilder(_db);

    [Fact]
    public async Task Admin_can_access_every_class_without_an_assignment()
    {
        var schoolClass = await _school.AddClassAsync();
        var sut = new ClassAccessService(_db, FakeCurrentUser.Admin());

        sut.AccessibleClassIds().Should().BeNull("null means no restriction");
        await sut.Invoking(s => s.EnsureCanAccessClassAsync(schoolClass.Id)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task Teacher_can_access_classes_she_teaches_in_and_her_homeroom_class_only()
    {
        var teacher = await _school.AddTeacherAsync();
        var subject = await _school.AddSubjectAsync();
        var taughtClass = await _school.AddClassAsync("ז'1");
        var homeroomClass = await _school.AddClassAsync("ח'1", 8, homeroomTeacher: teacher);
        var otherClass = await _school.AddClassAsync("ט'1", 9);
        await _school.AssignAsync(teacher, taughtClass, subject);

        var sut = new ClassAccessService(_db, FakeCurrentUser.Teacher(teacher.Id));

        sut.AccessibleClassIds()!.ToList().Should().BeEquivalentTo([taughtClass.Id, homeroomClass.Id]);
        await sut.Invoking(s => s.EnsureCanAccessClassAsync(otherClass.Id))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Teacher_does_not_get_access_through_another_teachers_assignment()
    {
        var teacher = await _school.AddTeacherAsync();
        var colleague = await _school.AddTeacherAsync("דוד הירש");
        var schoolClass = await _school.AddClassAsync();
        await _school.AssignAsync(colleague, schoolClass, await _school.AddSubjectAsync());

        var sut = new ClassAccessService(_db, FakeCurrentUser.Teacher(teacher.Id));

        await sut.Invoking(s => s.EnsureCanAccessClassAsync(schoolClass.Id))
            .Should().ThrowAsync<ForbiddenException>();
    }
}
