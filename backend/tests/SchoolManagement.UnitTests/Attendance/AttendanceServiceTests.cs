using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Attendance;

public class AttendanceServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly SchoolBuilder _school;

    public AttendanceServiceTests() => _school = new SchoolBuilder(_db);

    private AttendanceService CreateService(ICurrentUser user) =>
        new(_db, new ClassAccessService(_db, user), user, new FixedTimeProvider(Now), NullLogger<AttendanceService>.Instance);

    private static SaveAttendanceRequest Request(SchoolClass schoolClass, DateOnly date, params (Student Student, AttendanceStatus Status)[] records) => new()
    {
        ClassId = schoolClass.Id,
        Date = date,
        Records = records.Select(r => new SaveAttendanceEntry { StudentId = r.Student.Id, Status = r.Status }).ToList()
    };

    [Fact]
    public async Task Register_lists_active_students_with_their_status_for_the_day()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync(homeroomTeacher: teacher);
        var recorded = await _school.AddStudentAsync(schoolClass, "יוסף", "אברהם");
        await _school.AddStudentAsync(schoolClass, "דוד", "ברק");
        await _school.AddStudentAsync(schoolClass, "דן", "גל", isActive: false);
        await _school.AddAttendanceAsync(recorded, teacher, AttendanceStatus.Late); // recorded on 2026-10-01

        var register = await CreateService(FakeCurrentUser.Admin())
            .GetClassAttendanceAsync(new ClassAttendanceQuery { ClassId = schoolClass.Id, Date = new DateOnly(2026, 10, 1) });

        register.Students.Select(s => (s.FirstName, s.Status)).Should().Equal(
            ("יוסף", AttendanceStatus.Late),
            ("דוד", (AttendanceStatus?)null));
    }

    [Fact]
    public async Task Saving_creates_one_record_per_student()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync(homeroomTeacher: teacher);
        var yosef = await _school.AddStudentAsync(schoolClass, "יוסף");
        var david = await _school.AddStudentAsync(schoolClass, "דוד");

        var register = await CreateService(FakeCurrentUser.Teacher(teacher.Id)).SaveClassAttendanceAsync(
            Request(schoolClass, Today, (yosef, AttendanceStatus.Present), (david, AttendanceStatus.Absent)));

        register.Students.Should().OnlyContain(s => s.Status != null);
        var records = await _db.Attendance.ToListAsync();
        records.Should().HaveCount(2).And.OnlyContain(r => r.Date == Today && r.RecordedByUserId == teacher.Id);
    }

    [Fact]
    public async Task Saving_the_same_day_again_updates_instead_of_duplicating()
    {
        var schoolClass = await _school.AddClassAsync();
        var student = await _school.AddStudentAsync(schoolClass);
        var service = CreateService(FakeCurrentUser.Admin());

        await service.SaveClassAttendanceAsync(Request(schoolClass, Today, (student, AttendanceStatus.Absent)));
        await service.SaveClassAttendanceAsync(Request(schoolClass, Today, (student, AttendanceStatus.Excused)));

        var record = await _db.Attendance.SingleAsync();
        record.Status.Should().Be(AttendanceStatus.Excused);
    }

    [Fact]
    public async Task Attendance_cannot_be_recorded_for_a_future_date()
    {
        var schoolClass = await _school.AddClassAsync();
        var student = await _school.AddStudentAsync(schoolClass);

        var act = () => CreateService(FakeCurrentUser.Admin())
            .SaveClassAttendanceAsync(Request(schoolClass, Today.AddDays(1), (student, AttendanceStatus.Present)));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*future date*");
        (await _db.Attendance.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Students_of_another_class_are_rejected()
    {
        var schoolClass = await _school.AddClassAsync("ז'1");
        var otherClass = await _school.AddClassAsync("ז'2");
        var stranger = await _school.AddStudentAsync(otherClass);

        var act = () => CreateService(FakeCurrentUser.Admin())
            .SaveClassAttendanceAsync(Request(schoolClass, Today, (stranger, AttendanceStatus.Present)));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*not active students of this class*");
    }

    [Fact]
    public async Task Inactive_students_are_rejected()
    {
        var schoolClass = await _school.AddClassAsync();
        var inactive = await _school.AddStudentAsync(schoolClass, isActive: false);

        var act = () => CreateService(FakeCurrentUser.Admin())
            .SaveClassAttendanceAsync(Request(schoolClass, Today, (inactive, AttendanceStatus.Present)));

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task The_same_student_twice_in_one_request_is_rejected()
    {
        var schoolClass = await _school.AddClassAsync();
        var student = await _school.AddStudentAsync(schoolClass);

        var act = () => CreateService(FakeCurrentUser.Admin()).SaveClassAttendanceAsync(
            Request(schoolClass, Today, (student, AttendanceStatus.Present), (student, AttendanceStatus.Absent)));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*only once*");
    }

    [Fact]
    public async Task Teacher_cannot_record_attendance_for_a_class_she_does_not_teach()
    {
        var teacher = await _school.AddTeacherAsync();
        var otherClass = await _school.AddClassAsync();
        var student = await _school.AddStudentAsync(otherClass);

        var act = () => CreateService(FakeCurrentUser.Teacher(teacher.Id))
            .SaveClassAttendanceAsync(Request(otherClass, Today, (student, AttendanceStatus.Present)));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Subject_teacher_can_record_attendance_for_a_class_she_teaches_in()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        await _school.AssignAsync(teacher, schoolClass, await _school.AddSubjectAsync());
        var student = await _school.AddStudentAsync(schoolClass);

        await CreateService(FakeCurrentUser.Teacher(teacher.Id))
            .SaveClassAttendanceAsync(Request(schoolClass, Today, (student, AttendanceStatus.Present)));

        (await _db.Attendance.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Unknown_class_returns_not_found()
    {
        var act = () => CreateService(FakeCurrentUser.Admin())
            .GetClassAttendanceAsync(new ClassAttendanceQuery { ClassId = 999, Date = Today });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Report_counts_statuses_inside_the_range_only()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        var student = await _school.AddStudentAsync(schoolClass);
        // 2026-10-01 .. 2026-10-05; the report below covers only the first four days.
        await _school.AddAttendanceAsync(student, teacher,
            AttendanceStatus.Present, AttendanceStatus.Late, AttendanceStatus.Absent, AttendanceStatus.Excused,
            AttendanceStatus.Absent);

        var report = await CreateService(FakeCurrentUser.Admin()).GetReportAsync(new AttendanceReportQuery
        {
            ClassId = schoolClass.Id,
            From = new DateOnly(2026, 10, 1),
            To = new DateOnly(2026, 10, 4)
        });

        var row = report.Students.Should().ContainSingle().Subject;
        (row.Present, row.Late, row.Absent, row.Excused).Should().Be((1, 1, 1, 1));
        row.AttendanceRate.Should().Be(50); // present + late out of 4 recorded days
        report.AttendanceRate.Should().Be(50);
    }

    [Fact]
    public async Task Report_gives_no_rate_to_a_student_without_records()
    {
        var schoolClass = await _school.AddClassAsync();
        await _school.AddStudentAsync(schoolClass);

        var report = await CreateService(FakeCurrentUser.Admin()).GetReportAsync(new AttendanceReportQuery
        {
            ClassId = schoolClass.Id,
            From = Today.AddDays(-7),
            To = Today
        });

        report.Students.Single().AttendanceRate.Should().BeNull();
        report.AttendanceRate.Should().BeNull();
    }

    [Theory]
    [InlineData("2026-10-10", "2026-10-01")] // ends before it starts
    [InlineData("2025-01-01", "2026-01-02")] // 367 days, one more than MaxReportDays
    public async Task Report_rejects_invalid_ranges(string from, string to)
    {
        var schoolClass = await _school.AddClassAsync();

        var act = () => CreateService(FakeCurrentUser.Admin()).GetReportAsync(new AttendanceReportQuery
        {
            ClassId = schoolClass.Id,
            From = DateOnly.Parse(from),
            To = DateOnly.Parse(to)
        });

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Theory]
    [InlineData(0, 0, 0, null)]
    [InlineData(18, 1, 20, 95.0)]
    [InlineData(1, 0, 3, 33.3)]
    public void Attendance_rate_counts_present_and_late_days(int present, int late, int recordedDays, double? expected)
    {
        AttendanceRate.Calculate(present, late, recordedDays).Should().Be((decimal?)expected);
    }
}
