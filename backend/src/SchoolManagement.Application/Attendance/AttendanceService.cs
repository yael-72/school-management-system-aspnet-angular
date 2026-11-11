using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Attendance;

/// <summary>
/// Attendance is always handled per class: a teacher opens her class on a day and marks everyone.
/// Teachers may only work with the classes they can access (see IClassAccessService);
/// inactive students are left out of registers and reports.
/// </summary>
public class AttendanceService(
    IApplicationDbContext db,
    IClassAccessService classAccess,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<AttendanceService> logger) : IAttendanceService
{
    /// <summary>Longest range a report may cover, so one request cannot scan years of data.</summary>
    public const int MaxReportDays = 366;

    public async Task<ClassAttendanceDto> GetClassAttendanceAsync(
        ClassAttendanceQuery query,
        CancellationToken cancellationToken = default)
    {
        var className = await GetAccessibleClassNameAsync(query.ClassId, cancellationToken);
        var date = query.Date!.Value;

        // Each active student with that day's record, if there is one (a LEFT JOIN in SQL).
        var students = await db.Students
            .AsNoTracking()
            .Where(s => s.ClassId == query.ClassId && s.IsActive)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ThenBy(s => s.Id)
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                Record = s.AttendanceRecords
                    .Where(a => a.Date == date)
                    .Select(a => new { a.Status, a.Note })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var entries = students
            .Select(s => new AttendanceEntryDto(s.Id, s.FirstName, s.LastName, s.Record?.Status, s.Record?.Note))
            .ToList();

        return new ClassAttendanceDto(query.ClassId, className, date, entries);
    }

    public async Task<ClassAttendanceDto> SaveClassAttendanceAsync(
        SaveAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        await GetAccessibleClassNameAsync(request.ClassId, cancellationToken);
        var date = request.Date!.Value;

        if (date > Today())
        {
            throw new BusinessRuleException("Attendance cannot be recorded for a future date.");
        }

        var studentIds = request.Records.Select(r => r.StudentId).ToList();
        if (studentIds.Distinct().Count() != studentIds.Count)
        {
            throw new BusinessRuleException("Each student can appear only once in an attendance request.");
        }

        var activeStudentIds = await db.Students
            .Where(s => s.ClassId == request.ClassId && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var unknownStudentIds = studentIds.Except(activeStudentIds).ToList();
        if (unknownStudentIds.Count > 0)
        {
            throw new BusinessRuleException(
                $"Students {string.Join(", ", unknownStudentIds)} are not active students of this class.");
        }

        // Upsert: load the records that already exist for these students on this day, update them,
        // and add the ones that are missing. The unique index on (StudentId, Date) is the safety net.
        var existingRecords = await db.Attendance
            .Where(a => a.Date == date && studentIds.Contains(a.StudentId))
            .ToDictionaryAsync(a => a.StudentId, cancellationToken);

        foreach (var entry in request.Records)
        {
            if (!existingRecords.TryGetValue(entry.StudentId, out var record))
            {
                record = new AttendanceRecord { StudentId = entry.StudentId, Date = date };
                db.Attendance.Add(record);
            }

            record.Status = entry.Status!.Value;
            record.Note = string.IsNullOrWhiteSpace(entry.Note) ? null : entry.Note.Trim();
            record.RecordedByUserId = currentUser.Id;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Attendance of class {ClassId} on {Date} saved by user {UserId}: {Created} created, {Updated} updated",
            request.ClassId, date, currentUser.Id, request.Records.Count - existingRecords.Count, existingRecords.Count);

        return await GetClassAttendanceAsync(
            new ClassAttendanceQuery { ClassId = request.ClassId, Date = date },
            cancellationToken);
    }

    public async Task<AttendanceReportDto> GetReportAsync(
        AttendanceReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var className = await GetAccessibleClassNameAsync(query.ClassId, cancellationToken);
        var from = query.From!.Value;
        var to = query.To!.Value;

        if (from > to)
        {
            throw new BusinessRuleException("The start date must be on or before the end date.");
        }

        if (to.DayNumber - from.DayNumber + 1 > MaxReportDays)
        {
            throw new BusinessRuleException($"A report can cover at most {MaxReportDays} days.");
        }

        // One row per active student; each count is a small sub-query that SQL Server runs per student.
        var rows = await db.Students
            .AsNoTracking()
            .Where(s => s.ClassId == query.ClassId && s.IsActive)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ThenBy(s => s.Id)
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                Present = s.AttendanceRecords.Count(a => a.Date >= from && a.Date <= to && a.Status == AttendanceStatus.Present),
                Absent = s.AttendanceRecords.Count(a => a.Date >= from && a.Date <= to && a.Status == AttendanceStatus.Absent),
                Late = s.AttendanceRecords.Count(a => a.Date >= from && a.Date <= to && a.Status == AttendanceStatus.Late),
                Excused = s.AttendanceRecords.Count(a => a.Date >= from && a.Date <= to && a.Status == AttendanceStatus.Excused)
            })
            .ToListAsync(cancellationToken);

        var students = rows
            .Select(r => new StudentAttendanceSummaryDto(
                r.Id, r.FirstName, r.LastName, r.Present, r.Absent, r.Late, r.Excused,
                AttendanceRate.Calculate(r.Present, r.Late, r.Present + r.Absent + r.Late + r.Excused)))
            .ToList();

        var classRate = AttendanceRate.Calculate(
            students.Sum(s => s.Present),
            students.Sum(s => s.Late),
            students.Sum(s => s.Present + s.Absent + s.Late + s.Excused));

        return new AttendanceReportDto(query.ClassId, className, from, to, classRate, students);
    }

    /// <summary>404 when the class does not exist, 403 when the current user may not access it.</summary>
    private async Task<string> GetAccessibleClassNameAsync(int classId, CancellationToken cancellationToken)
    {
        var className = await db.Classes
            .Where(c => c.Id == classId)
            .Select(c => c.Name)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Class", classId);

        await classAccess.EnsureCanAccessClassAsync(classId, cancellationToken);
        return className;
    }

    // Same convention as the rest of the API: dates are compared in UTC.
    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
