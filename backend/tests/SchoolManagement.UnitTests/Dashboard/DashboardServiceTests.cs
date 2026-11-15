using FluentAssertions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Application.Dashboard;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Dashboard;

public class DashboardServiceTests
{
    // Tuesday. The school week started on Sunday 2026-11-08.
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 10);

    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly SchoolBuilder _school;

    public DashboardServiceTests() => _school = new SchoolBuilder(_db);

    private DashboardService CreateService(ICurrentUser user) =>
        new(_db, new ClassAccessService(_db, user), new FixedTimeProvider(Now));

    private async Task AddAttendanceAsync(Student student, DateOnly date, AttendanceStatus status, User recordedBy)
    {
        _db.Attendance.Add(new AttendanceRecord
        {
            StudentId = student.Id,
            Date = date,
            Status = status,
            RecordedByUserId = recordedBy.Id
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Teacher_stats_count_only_her_classes_and_active_students()
    {
        var teacher = await _school.AddTeacherAsync();
        var myClass = await _school.AddClassAsync("ז'1", homeroomTeacher: teacher);
        var otherClass = await _school.AddClassAsync("ח'1", 8);
        await _school.AddStudentAsync(myClass, "יוסף");
        await _school.AddStudentAsync(myClass, "דוד", isActive: false);
        await _school.AddStudentAsync(otherClass, "משה");

        var teacherStats = await CreateService(FakeCurrentUser.Teacher(teacher.Id)).GetStatsAsync();
        var adminStats = await CreateService(FakeCurrentUser.Admin()).GetStatsAsync();

        (teacherStats.StudentCount, teacherStats.ClassCount).Should().Be((1, 1));
        (adminStats.StudentCount, adminStats.ClassCount).Should().Be((2, 2));
    }

    [Fact]
    public async Task Stats_include_todays_attendance_rate_and_the_exams_of_this_week()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        var subject = await _school.AddSubjectAsync();
        var present = await _school.AddStudentAsync(schoolClass, "יוסף");
        var late = await _school.AddStudentAsync(schoolClass, "דוד");
        var absent = await _school.AddStudentAsync(schoolClass, "משה");
        await AddAttendanceAsync(present, Today, AttendanceStatus.Present, teacher);
        await AddAttendanceAsync(late, Today, AttendanceStatus.Late, teacher);
        await AddAttendanceAsync(absent, Today, AttendanceStatus.Absent, teacher);
        await AddAttendanceAsync(absent, Today.AddDays(-1), AttendanceStatus.Present, teacher); // not today

        await _school.AddAssessmentAsync(schoolClass, subject, teacher, date: new DateOnly(2026, 11, 8)); // Sunday
        await _school.AddAssessmentAsync(schoolClass, subject, teacher, date: new DateOnly(2026, 11, 14)); // Saturday
        await _school.AddAssessmentAsync(schoolClass, subject, teacher, date: new DateOnly(2026, 11, 15)); // next week

        var stats = await CreateService(FakeCurrentUser.Admin()).GetStatsAsync();

        stats.TodayAttendanceRate.Should().Be(66.7m); // 2 of 3 came (present or late)
        stats.ExamsThisWeek.Should().Be(2);
    }

    [Fact]
    public async Task Frequent_absences_lists_students_absent_often_in_the_last_30_days()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        var often = await _school.AddStudentAsync(schoolClass, "יוסף");
        var sometimes = await _school.AddStudentAsync(schoolClass, "דוד");
        var longAgo = await _school.AddStudentAsync(schoolClass, "משה");

        for (var i = 0; i < DashboardService.FrequentAbsenceThreshold; i++)
        {
            await AddAttendanceAsync(often, Today.AddDays(-i), AttendanceStatus.Absent, teacher);
            await AddAttendanceAsync(longAgo, Today.AddDays(-40 - i), AttendanceStatus.Absent, teacher);
        }
        await AddAttendanceAsync(sometimes, Today, AttendanceStatus.Absent, teacher);

        var stats = await CreateService(FakeCurrentUser.Admin()).GetStatsAsync();

        stats.FrequentAbsences.Should().ContainSingle()
            .Which.Should().Be(new FrequentAbsenceDto(often.Id, "יוסף", "לוי", "ז'1", DashboardService.FrequentAbsenceThreshold));
    }

    [Fact]
    public async Task Grades_trend_has_a_point_for_every_month_including_empty_ones()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        var subject = await _school.AddSubjectAsync();
        var student = await _school.AddStudentAsync(schoolClass);
        var september = await _school.AddAssessmentAsync(schoolClass, subject, teacher, maxScore: 20, date: new DateOnly(2026, 9, 15));
        var november = await _school.AddAssessmentAsync(schoolClass, subject, teacher, date: new DateOnly(2026, 11, 2));
        await _school.AddScoreAsync(september, student, 16); // 80%
        await _school.AddScoreAsync(november, student, 90);

        var trend = await CreateService(FakeCurrentUser.Admin()).GetGradesTrendAsync(3);

        trend.Should().Equal(
            new MonthlyAverageDto(2026, 9, 80),
            new MonthlyAverageDto(2026, 10, null),
            new MonthlyAverageDto(2026, 11, 90));
    }

    [Fact]
    public async Task Weekly_attendance_has_a_bar_for_each_school_day_of_the_last_week()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        var student = await _school.AddStudentAsync(schoolClass);
        await AddAttendanceAsync(student, Today, AttendanceStatus.Late, teacher);

        var week = await CreateService(FakeCurrentUser.Admin()).GetWeeklyAttendanceAsync();

        // 4–10 November without Friday the 6th and Saturday the 7th.
        week.Select(d => d.Date.Day).Should().Equal(4, 5, 8, 9, 10);
        week[^1].Should().Be(new DailyAttendanceDto(Today, 0, 1, 0, 0, 100));
        week[0].AttendanceRate.Should().BeNull("nothing was recorded that day");
    }

    [Fact]
    public async Task Grade_distribution_counts_scores_per_grade_band()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        var subject = await _school.AddSubjectAsync();
        var assessment = await _school.AddAssessmentAsync(schoolClass, subject, teacher);
        decimal?[] scores = [95, 90, 80, 60, 40, null];
        foreach (var score in scores)
        {
            await _school.AddScoreAsync(assessment, await _school.AddStudentAsync(schoolClass), score);
        }
        var inactive = await _school.AddStudentAsync(schoolClass, isActive: false);
        await _school.AddScoreAsync(assessment, inactive, 10);

        var distribution = await CreateService(FakeCurrentUser.Admin()).GetGradeDistributionAsync();

        distribution.Should().Be(new GradeDistributionDto(Excellent: 2, Good: 1, Fair: 1, Low: 1));
    }

    [Fact]
    public async Task Top_students_are_ordered_by_average_and_skip_students_without_scores()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        var subject = await _school.AddSubjectAsync();
        var assessment = await _school.AddAssessmentAsync(schoolClass, subject, teacher);
        var good = await _school.AddStudentAsync(schoolClass, "יוסף");
        var best = await _school.AddStudentAsync(schoolClass, "דוד");
        await _school.AddStudentAsync(schoolClass, "משה"); // no scores
        await _school.AddScoreAsync(assessment, good, 85);
        await _school.AddScoreAsync(assessment, best, 97);

        var top = await CreateService(FakeCurrentUser.Admin()).GetTopStudentsAsync(5);

        top.Select(s => (s.FirstName, s.AverageScore)).Should().Equal(("דוד", 97m), ("יוסף", 85m));
    }

    [Fact]
    public async Task Recent_activity_merges_created_and_graded_assessments_newest_first()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        var subject = await _school.AddSubjectAsync();
        var student = await _school.AddStudentAsync(schoolClass);
        var older = await _school.AddAssessmentAsync(schoolClass, subject, teacher, title: "מבחן ישן");
        var newer = await _school.AddAssessmentAsync(schoolClass, subject, teacher, title: "מבחן חדש");
        older.CreatedAt = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        newer.CreatedAt = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);
        _db.Scores.Add(new Score
        {
            AssessmentId = older.Id,
            StudentId = student.Id,
            Value = 90,
            UpdatedAt = new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc)
        });
        await _db.SaveChangesAsync();

        var activity = await CreateService(FakeCurrentUser.Admin()).GetRecentActivityAsync(8);

        activity.Select(a => (a.Kind, a.Title)).Should().Equal(
            (ActivityKind.ScoresEntered, "מבחן ישן"),
            (ActivityKind.AssessmentCreated, "מבחן חדש"),
            (ActivityKind.AssessmentCreated, "מבחן ישן"));
        activity[1].UserName.Should().Be(teacher.FullName);
    }
}
