using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Domain.Calendar;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Dashboard;

/// <summary>
/// Calculates the dashboard numbers in the database (COUNT, AVG, GROUP BY), so only small results
/// travel to the API – never thousands of score or attendance rows.
///
/// Every query starts from one of the scoped helpers below (ActiveStudents, GradedScores,
/// Assessments), which apply the role rule: admins see the whole school, teachers their classes.
/// </summary>
public class DashboardService(
    IApplicationDbContext db,
    IClassAccessService classAccess,
    TimeProvider timeProvider) : IDashboardService
{
    /// <summary>"Frequent absences" looks at this many recent days...</summary>
    public const int AbsenceWindowDays = 30;

    /// <summary>...and lists students absent at least this many times in them.</summary>
    public const int FrequentAbsenceThreshold = 4;

    private const int FrequentAbsenceListSize = 5;

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var today = Today();
        var students = ActiveStudents();

        var studentCount = await students.CountAsync(cancellationToken);
        var classCount = await Classes().CountAsync(cancellationToken);

        var todayByStatus = await AttendanceOfActiveStudents()
            .Where(a => a.Date == today)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        var todayRate = AttendanceRate.Calculate(
            todayByStatus.GetValueOrDefault(AttendanceStatus.Present),
            todayByStatus.GetValueOrDefault(AttendanceStatus.Late),
            todayByStatus.Values.Sum());

        var averageScore = await GradedScores()
            .AverageAsync(s => s.Value * 100 / s.Assessment.MaxScore, cancellationToken);

        var weekStart = SchoolCalendar.StartOfWeek(today);
        var weekEnd = weekStart.AddDays(6);
        var examsThisWeek = await Assessments()
            .CountAsync(a => a.Date >= weekStart && a.Date <= weekEnd, cancellationToken);

        var absenceFrom = today.AddDays(-(AbsenceWindowDays - 1));
        var frequentAbsences = await students
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                ClassName = s.Class.Name,
                Absences = s.AttendanceRecords.Count(a =>
                    a.Status == AttendanceStatus.Absent && a.Date >= absenceFrom && a.Date <= today)
            })
            .Where(s => s.Absences >= FrequentAbsenceThreshold)
            .OrderByDescending(s => s.Absences)
            .ThenBy(s => s.LastName)
            .ThenBy(s => s.Id)
            .Take(FrequentAbsenceListSize)
            .ToListAsync(cancellationToken);

        return new DashboardStatsDto(
            studentCount,
            classCount,
            todayRate,
            Round(averageScore),
            examsThisWeek,
            frequentAbsences
                .Select(s => new FrequentAbsenceDto(s.Id, s.FirstName, s.LastName, s.ClassName, s.Absences))
                .ToList());
    }

    public async Task<IReadOnlyList<MonthlyAverageDto>> GetGradesTrendAsync(
        int months,
        CancellationToken cancellationToken = default)
    {
        var today = Today();
        var firstMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-(months - 1));

        var averages = await GradedScores()
            .Where(s => s.Assessment.Date >= firstMonth && s.Assessment.Date <= today)
            .GroupBy(s => new { s.Assessment.Date.Year, s.Assessment.Date.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Average = g.Average(s => s.Value * 100 / s.Assessment.MaxScore)
            })
            .ToListAsync(cancellationToken);

        // Every month in the range gets a point, so the chart's x-axis has no gaps.
        return Enumerable.Range(0, months)
            .Select(i => firstMonth.AddMonths(i))
            .Select(month => new MonthlyAverageDto(
                month.Year,
                month.Month,
                Round(averages.SingleOrDefault(a => a.Year == month.Year && a.Month == month.Month)?.Average)))
            .ToList();
    }

    public async Task<IReadOnlyList<DailyAttendanceDto>> GetWeeklyAttendanceAsync(CancellationToken cancellationToken = default)
    {
        var today = Today();
        var from = today.AddDays(-6);

        var counts = await AttendanceOfActiveStudents()
            .Where(a => a.Date >= from && a.Date <= today)
            .GroupBy(a => new { a.Date, a.Status })
            .Select(g => new { g.Key.Date, g.Key.Status, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(DateOnly date, AttendanceStatus status) =>
            counts.SingleOrDefault(c => c.Date == date && c.Status == status)?.Count ?? 0;

        return Enumerable.Range(0, 7)
            .Select(i => from.AddDays(i))
            .Where(SchoolCalendar.IsSchoolDay)
            .Select(date =>
            {
                var present = CountOf(date, AttendanceStatus.Present);
                var late = CountOf(date, AttendanceStatus.Late);
                var absent = CountOf(date, AttendanceStatus.Absent);
                var excused = CountOf(date, AttendanceStatus.Excused);
                return new DailyAttendanceDto(
                    date, present, late, absent, excused,
                    AttendanceRate.Calculate(present, late, present + late + absent + excused));
            })
            .ToList();
    }

    public async Task<GradeDistributionDto> GetGradeDistributionAsync(CancellationToken cancellationToken = default)
    {
        // One query with four conditional counts: SELECT COUNT(CASE WHEN ... >= 90 ...), ...
        var counts = await GradedScores()
            .Select(s => s.Value!.Value * 100 / s.Assessment.MaxScore)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Excellent = g.Count(p => p >= GradeBands.Excellent),
                Good = g.Count(p => p >= GradeBands.Good && p < GradeBands.Excellent),
                Fair = g.Count(p => p >= GradeBands.Fair && p < GradeBands.Good),
                Low = g.Count(p => p < GradeBands.Fair)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return counts is null
            ? new GradeDistributionDto(0, 0, 0, 0)
            : new GradeDistributionDto(counts.Excellent, counts.Good, counts.Fair, counts.Low);
    }

    public async Task<IReadOnlyList<TopStudentDto>> GetTopStudentsAsync(int count, CancellationToken cancellationToken = default)
    {
        var students = await ActiveStudents()
            .Where(s => s.Scores.Any(sc => sc.Value != null))
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                ClassName = s.Class.Name,
                Average = s.Scores
                    .Where(sc => sc.Value != null)
                    .Average(sc => sc.Value * 100 / sc.Assessment.MaxScore)
            })
            .OrderByDescending(s => s.Average)
            .ThenBy(s => s.Id)
            .Take(count)
            .ToListAsync(cancellationToken);

        return students
            .Select(s => new TopStudentDto(s.Id, s.FirstName, s.LastName, s.ClassName, Round(s.Average) ?? 0))
            .ToList();
    }

    public async Task<IReadOnlyList<RecentActivityDto>> GetRecentActivityAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        var assessments = Assessments();

        var created = await assessments
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Take(count)
            .Select(a => new RecentActivityDto(
                ActivityKind.AssessmentCreated,
                a.Id,
                a.Title,
                a.Class.Name,
                a.Subject.Name,
                a.CreatedAt,
                a.CreatedByUser.FullName))
            .ToListAsync(cancellationToken);

        // An assessment counts as "graded" at the time its latest score was saved.
        var graded = await assessments
            .Where(a => a.Scores.Any())
            .Select(a => new
            {
                a.Id,
                a.Title,
                ClassName = a.Class.Name,
                SubjectName = a.Subject.Name,
                LastGradedAt = a.Scores.Max(s => s.UpdatedAt)
            })
            .OrderByDescending(a => a.LastGradedAt)
            .ThenByDescending(a => a.Id)
            .Take(count)
            .ToListAsync(cancellationToken);

        return created
            .Concat(graded.Select(a => new RecentActivityDto(
                ActivityKind.ScoresEntered, a.Id, a.Title, a.ClassName, a.SubjectName, a.LastGradedAt, null)))
            .OrderByDescending(a => a.OccurredAt)
            .Take(count)
            .ToList();
    }

    // ---------- scoped starting points ----------

    private IQueryable<SchoolClass> Classes()
    {
        var classes = db.Classes.AsNoTracking();
        var accessibleClassIds = classAccess.AccessibleClassIds();
        return accessibleClassIds is null ? classes : classes.Where(c => accessibleClassIds.Contains(c.Id));
    }

    private IQueryable<Student> ActiveStudents()
    {
        var students = db.Students.AsNoTracking().Where(s => s.IsActive);
        var accessibleClassIds = classAccess.AccessibleClassIds();
        return accessibleClassIds is null ? students : students.Where(s => accessibleClassIds.Contains(s.ClassId));
    }

    /// <summary>Scores that have a value, of active students in the accessible classes.</summary>
    private IQueryable<Score> GradedScores()
    {
        var scores = db.Scores.AsNoTracking().Where(s => s.Value != null && s.Student.IsActive);
        var accessibleClassIds = classAccess.AccessibleClassIds();
        return accessibleClassIds is null ? scores : scores.Where(s => accessibleClassIds.Contains(s.Student.ClassId));
    }

    private IQueryable<AttendanceRecord> AttendanceOfActiveStudents()
    {
        var records = db.Attendance.AsNoTracking().Where(a => a.Student.IsActive);
        var accessibleClassIds = classAccess.AccessibleClassIds();
        return accessibleClassIds is null ? records : records.Where(a => accessibleClassIds.Contains(a.Student.ClassId));
    }

    private IQueryable<Assessment> Assessments()
    {
        var assessments = db.Assessments.AsNoTracking();
        var accessibleClassIds = classAccess.AccessibleClassIds();
        return accessibleClassIds is null ? assessments : assessments.Where(a => accessibleClassIds.Contains(a.ClassId));
    }

    private static decimal? Round(decimal? value) => value is null ? null : Math.Round(value.Value, 1);

    // Same convention as the rest of the API: dates are compared in UTC.
    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}

/// <summary>
/// The grade bands used to color scores. The Angular ScoreBadge uses the same numbers;
/// if you change them here, change them there too.
/// </summary>
public static class GradeBands
{
    public const decimal Excellent = 90;
    public const decimal Good = 75;
    public const decimal Fair = 55;
}
