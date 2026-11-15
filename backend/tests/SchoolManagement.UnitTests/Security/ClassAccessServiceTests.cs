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

    [Fact]
    public async Task Teacher_can_grade_only_the_subjects_she_teaches_in_a_class()
    {
        var teacher = await _school.AddTeacherAsync();
        var math = await _school.AddSubjectAsync("מתמטיקה");
        var english = await _school.AddSubjectAsync("אנגלית");
        var schoolClass = await _school.AddClassAsync(homeroomTeacher: teacher);
        await _school.AssignAsync(teacher, schoolClass, math);

        var sut = new ClassAccessService(_db, FakeCurrentUser.Teacher(teacher.Id));

        (await sut.GetGradableSubjectsAsync()).Should().BeEquivalentTo([new ClassSubject(schoolClass.Id, math.Id)]);
        await sut.Invoking(s => s.EnsureCanGradeAsync(schoolClass.Id, math.Id)).Should().NotThrowAsync();
        // Being the homeroom teacher lets her see the class, not grade subjects taught by others.
        await sut.Invoking(s => s.EnsureCanGradeAsync(schoolClass.Id, english.Id))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Admin_can_grade_every_subject()
    {
        var schoolClass = await _school.AddClassAsync();
        var subject = await _school.AddSubjectAsync();
        var sut = new ClassAccessService(_db, FakeCurrentUser.Admin());

        (await sut.GetGradableSubjectsAsync()).Should().BeNull("null means every subject in every class");
        await sut.Invoking(s => s.EnsureCanGradeAsync(schoolClass.Id, subject.Id)).Should().NotThrowAsync();
    }
}
