using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Classes;

public class ClassServiceTests
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly SchoolBuilder _school;

    public ClassServiceTests() => _school = new SchoolBuilder(_db);

    private ClassService CreateService(ICurrentUser user) =>
        new(_db, new ClassAccessService(_db, user), NullLogger<ClassService>.Instance);

    private static SaveClassRequest NewClassRequest(string name = "ז'3", int? homeroomTeacherId = null) => new()
    {
        Name = name,
        GradeLevel = 7,
        SchoolYear = "2026-2027",
        HomeroomTeacherId = homeroomTeacherId
    };

    [Fact]
    public async Task Teacher_only_sees_the_classes_she_may_access()
    {
        var teacher = await _school.AddTeacherAsync();
        var myClass = await _school.AddClassAsync("ז'1");
        await _school.AddClassAsync("ז'2");
        await _school.AssignAsync(teacher, myClass, await _school.AddSubjectAsync());

        var result = await CreateService(FakeCurrentUser.Teacher(teacher.Id)).GetClassesAsync(new ClassListQuery());

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(myClass.Id);
    }

    [Fact]
    public async Task List_counts_only_active_students_and_averages_scores_as_percentages()
    {
        var teacher = await _school.AddTeacherAsync();
        var subject = await _school.AddSubjectAsync();
        var schoolClass = await _school.AddClassAsync();
        var yosef = await _school.AddStudentAsync(schoolClass, "יוסף");
        var binyamin = await _school.AddStudentAsync(schoolClass, "בנימין");
        var leftSchool = await _school.AddStudentAsync(schoolClass, "דן", isActive: false);

        // 40/50 = 80%, 90/100 = 90%. The inactive student's 0 and the ungraded score must be ignored.
        var quiz = await _school.AddAssessmentAsync(schoolClass, subject, teacher, maxScore: 50);
        var exam = await _school.AddAssessmentAsync(schoolClass, subject, teacher, maxScore: 100);
        await _school.AddScoreAsync(quiz, yosef, 40);
        await _school.AddScoreAsync(exam, binyamin, 90);
        await _school.AddScoreAsync(exam, yosef, null);
        await _school.AddScoreAsync(exam, leftSchool, 0);

        var result = await CreateService(FakeCurrentUser.Admin()).GetClassesAsync(new ClassListQuery());

        var card = result.Items.Single();
        card.StudentCount.Should().Be(2);
        card.AverageScore.Should().Be(85);
    }

    [Fact]
    public async Task List_filters_by_grade_level_and_pages_results()
    {
        await _school.AddClassAsync("ז'1", 7);
        await _school.AddClassAsync("ז'2", 7);
        await _school.AddClassAsync("ז'3", 7);
        await _school.AddClassAsync("ח'1", 8);

        var result = await CreateService(FakeCurrentUser.Admin())
            .GetClassesAsync(new ClassListQuery { GradeLevel = 7, Page = 2, PageSize = 2 });

        result.TotalCount.Should().Be(3);
        result.Items.Select(c => c.Name).Should().Equal("ז'3");
    }

    [Fact]
    public async Task Teacher_gets_403_for_a_class_she_does_not_teach()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();

        var act = () => CreateService(FakeCurrentUser.Teacher(teacher.Id)).GetClassAsync(schoolClass.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Getting_a_missing_class_throws_not_found()
    {
        var act = () => CreateService(FakeCurrentUser.Admin()).GetClassAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_name_in_the_same_school_year()
    {
        await _school.AddClassAsync("ז'1");

        var act = () => CreateService(FakeCurrentUser.Admin()).CreateClassAsync(NewClassRequest(" ז'1 "));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task Create_rejects_an_unknown_homeroom_teacher()
    {
        var act = () => CreateService(FakeCurrentUser.Admin()).CreateClassAsync(NewClassRequest(homeroomTeacherId: 999));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Homeroom teacher*");
    }

    [Fact]
    public async Task Create_saves_the_class_with_its_homeroom_teacher()
    {
        var teacher = await _school.AddTeacherAsync("דוד הירש");

        var created = await CreateService(FakeCurrentUser.Admin()).CreateClassAsync(NewClassRequest(homeroomTeacherId: teacher.Id));

        created.Name.Should().Be("ז'3");
        created.HomeroomTeacherName.Should().Be("דוד הירש");
        _db.Classes.Should().ContainSingle(c => c.Id == created.Id);
    }

    [Fact]
    public async Task Delete_is_rejected_while_the_class_has_active_students()
    {
        var schoolClass = await _school.AddClassAsync();
        await _school.AddStudentAsync(schoolClass);

        var act = () => CreateService(FakeCurrentUser.Admin()).DeleteClassAsync(schoolClass.Id);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*1 active students*");
    }

    [Fact]
    public async Task Delete_is_rejected_when_the_class_has_former_students()
    {
        var schoolClass = await _school.AddClassAsync();
        await _school.AddStudentAsync(schoolClass, isActive: false);

        var act = () => CreateService(FakeCurrentUser.Admin()).DeleteClassAsync(schoolClass.Id);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*history*");
    }

    [Fact]
    public async Task Delete_removes_an_empty_class()
    {
        var schoolClass = await _school.AddClassAsync();

        await CreateService(FakeCurrentUser.Admin()).DeleteClassAsync(schoolClass.Id);

        _db.Classes.Should().BeEmpty();
    }
}
