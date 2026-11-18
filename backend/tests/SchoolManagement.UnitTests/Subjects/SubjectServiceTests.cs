using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Subjects;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Subjects;

public class SubjectServiceTests
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly SchoolBuilder _school;
    private readonly SubjectService _service;

    public SubjectServiceTests()
    {
        _school = new SchoolBuilder(_db);
        _service = new SubjectService(_db, NullLogger<SubjectService>.Instance);
    }

    [Fact]
    public async Task Subjects_are_created_renamed_and_listed_alphabetically()
    {
        var history = await _service.CreateSubjectAsync(new SaveSubjectRequest { Name = " היסטוריה " });
        await _service.CreateSubjectAsync(new SaveSubjectRequest { Name = "אנגלית" });

        await _service.UpdateSubjectAsync(history.Id, new SaveSubjectRequest { Name = "תולדות עם ישראל" });

        (await _service.GetSubjectsAsync()).Select(s => s.Name).Should().Equal("אנגלית", "תולדות עם ישראל");
    }

    [Fact]
    public async Task Subject_names_are_unique()
    {
        await _service.CreateSubjectAsync(new SaveSubjectRequest { Name = "מתמטיקה" });

        var act = () => _service.CreateSubjectAsync(new SaveSubjectRequest { Name = "מתמטיקה" });

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task A_subject_that_is_taught_or_has_assessments_cannot_be_deleted()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        var taught = await _school.AddSubjectAsync("מתמטיקה");
        var examined = await _school.AddSubjectAsync("אנגלית");
        await _school.AssignAsync(teacher, schoolClass, taught);
        await _school.AddAssessmentAsync(schoolClass, examined, teacher);

        await _service.Invoking(s => s.DeleteSubjectAsync(taught.Id)).Should().ThrowAsync<BusinessRuleException>().WithMessage("*still taught*");
        await _service.Invoking(s => s.DeleteSubjectAsync(examined.Id)).Should().ThrowAsync<BusinessRuleException>().WithMessage("*has assessments*");
    }

    [Fact]
    public async Task An_unused_subject_is_deleted()
    {
        var subject = await _school.AddSubjectAsync("ספרות");

        await _service.DeleteSubjectAsync(subject.Id);

        (await _db.Subjects.AnyAsync()).Should().BeFalse();
    }
}
