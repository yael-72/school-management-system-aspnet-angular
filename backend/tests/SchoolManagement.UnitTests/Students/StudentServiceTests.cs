using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Application.Students;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Students;

public class StudentServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly SchoolBuilder _school;

    public StudentServiceTests() => _school = new SchoolBuilder(_db);

    private StudentService CreateService(ICurrentUser user) =>
        new(_db, new ClassAccessService(_db, user), new FixedTimeProvider(Now), NullLogger<StudentService>.Instance);

    private static SaveStudentRequest NewStudentRequest(int classId, DateOnly? birthDate = null) => new()
    {
        FirstName = " אברהם ",
        LastName = "כהן",
        ClassId = classId,
        BirthDate = birthDate,
        ParentPhone = "050-1234567"
    };

    [Fact]
    public async Task List_hides_inactive_students()
    {
        var schoolClass = await _school.AddClassAsync();
        await _school.AddStudentAsync(schoolClass, "יוסף");
        await _school.AddStudentAsync(schoolClass, "דן", isActive: false);

        var result = await CreateService(FakeCurrentUser.Admin()).GetStudentsAsync(new StudentListQuery());

        result.Items.Select(s => s.FirstName).Should().Equal("יוסף");
    }

    [Fact]
    public async Task List_searches_by_full_name()
    {
        var schoolClass = await _school.AddClassAsync();
        await _school.AddStudentAsync(schoolClass, "יוסף", "לוי");
        await _school.AddStudentAsync(schoolClass, "יוסף", "כהן");

        var result = await CreateService(FakeCurrentUser.Admin())
            .GetStudentsAsync(new StudentListQuery { Search = "יוסף כהן" });

        result.Items.Should().ContainSingle(s => s.LastName == "כהן");
    }

    [Fact]
    public async Task Teacher_only_sees_students_of_her_classes()
    {
        var teacher = await _school.AddTeacherAsync();
        var myClass = await _school.AddClassAsync("ז'1", homeroomTeacher: teacher);
        var otherClass = await _school.AddClassAsync("ז'2");
        await _school.AddStudentAsync(myClass, "יוסף");
        await _school.AddStudentAsync(otherClass, "דן");

        var result = await CreateService(FakeCurrentUser.Teacher(teacher.Id)).GetStudentsAsync(new StudentListQuery());

        result.Items.Select(s => s.FirstName).Should().Equal("יוסף");
    }

    [Fact]
    public async Task Teacher_gets_403_when_filtering_by_a_class_she_does_not_teach()
    {
        var teacher = await _school.AddTeacherAsync();
        var otherClass = await _school.AddClassAsync();

        var act = () => CreateService(FakeCurrentUser.Teacher(teacher.Id))
            .GetStudentsAsync(new StudentListQuery { ClassId = otherClass.Id });

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Teacher_gets_403_for_a_student_of_another_class()
    {
        var teacher = await _school.AddTeacherAsync();
        var student = await _school.AddStudentAsync(await _school.AddClassAsync());

        var act = () => CreateService(FakeCurrentUser.Teacher(teacher.Id)).GetStudentAsync(student.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Create_trims_names_and_saves_the_student()
    {
        var schoolClass = await _school.AddClassAsync();

        var created = await CreateService(FakeCurrentUser.Admin()).CreateStudentAsync(NewStudentRequest(schoolClass.Id));

        created.FirstName.Should().Be("אברהם");
        created.ClassName.Should().Be(schoolClass.Name);
    }

    [Fact]
    public async Task Create_rejects_a_birth_date_in_the_future()
    {
        var schoolClass = await _school.AddClassAsync();
        var tomorrow = DateOnly.FromDateTime(Now.UtcDateTime).AddDays(1);

        var act = () => CreateService(FakeCurrentUser.Admin()).CreateStudentAsync(NewStudentRequest(schoolClass.Id, tomorrow));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*future*");
    }

    [Fact]
    public async Task Create_rejects_an_unknown_class()
    {
        var act = () => CreateService(FakeCurrentUser.Admin()).CreateStudentAsync(NewStudentRequest(classId: 999));

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Delete_is_a_soft_delete_that_hides_the_student()
    {
        var student = await _school.AddStudentAsync(await _school.AddClassAsync());
        var sut = CreateService(FakeCurrentUser.Admin());

        await sut.DeleteStudentAsync(student.Id);

        _db.Students.Single().IsActive.Should().BeFalse("the row and its history are kept");
        await sut.Invoking(s => s.GetStudentAsync(student.Id)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Summary_calculates_average_attendance_rate_and_recent_scores()
    {
        var teacher = await _school.AddTeacherAsync();
        var subject = await _school.AddSubjectAsync();
        var schoolClass = await _school.AddClassAsync();
        var student = await _school.AddStudentAsync(schoolClass);

        var older = await _school.AddAssessmentAsync(schoolClass, subject, teacher, 50, new DateOnly(2026, 9, 1), "מבחן 1");
        var newer = await _school.AddAssessmentAsync(schoolClass, subject, teacher, 100, new DateOnly(2026, 10, 1), "מבחן 2");
        await _school.AddScoreAsync(older, student, 35);  // 70%
        await _school.AddScoreAsync(newer, student, 90);  // 90%

        // 3 of 4 days present or late = 75%.
        await _school.AddAttendanceAsync(student, teacher,
            AttendanceStatus.Present, AttendanceStatus.Late, AttendanceStatus.Present, AttendanceStatus.Absent);

        var summary = await CreateService(FakeCurrentUser.Admin()).GetSummaryAsync(student.Id);

        summary.AverageScore.Should().Be(80);
        summary.AttendanceRate.Should().Be(75);
        summary.AbsenceCount.Should().Be(1);
        summary.LateCount.Should().Be(1);
        summary.RecentScores.Select(s => s.AssessmentTitle).Should().Equal("מבחן 2", "מבחן 1");
        summary.RecentScores[1].Percent.Should().Be(70);
    }

    [Fact]
    public async Task Summary_has_no_rates_when_nothing_was_recorded_yet()
    {
        var student = await _school.AddStudentAsync(await _school.AddClassAsync());

        var summary = await CreateService(FakeCurrentUser.Admin()).GetSummaryAsync(student.Id);

        summary.AverageScore.Should().BeNull();
        summary.AttendanceRate.Should().BeNull();
        summary.RecentScores.Should().BeEmpty();
    }
}
