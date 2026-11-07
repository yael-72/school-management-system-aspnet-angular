using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Extensions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Students;

/// <summary>
/// Inactive (soft-deleted) students are hidden everywhere: in lists, by id and in summaries.
/// Teachers only see students of the classes they may access (see IClassAccessService).
/// Creating, updating and deleting is for admins only; the controller enforces that.
/// </summary>
public class StudentService(
    IApplicationDbContext db,
    IClassAccessService classAccess,
    TimeProvider timeProvider,
    ILogger<StudentService> logger) : IStudentService
{
    private const int RecentScoresCount = 5;

    public async Task<PagedResult<StudentListItemDto>> GetStudentsAsync(
        StudentListQuery query,
        CancellationToken cancellationToken = default)
    {
        var students = db.Students.AsNoTracking().Where(s => s.IsActive);

        if (query.ClassId is { } classId)
        {
            // A teacher asking for a class she does not teach gets 403, not a silently empty list.
            await classAccess.EnsureCanAccessClassAsync(classId, cancellationToken);
            students = students.Where(s => s.ClassId == classId);
        }

        var accessibleClassIds = classAccess.AccessibleClassIds();
        if (accessibleClassIds is not null)
        {
            students = students.Where(s => accessibleClassIds.Contains(s.ClassId));
        }

        if (query.SearchTerm is { } search)
        {
            students = students.Where(s => (s.FirstName + " " + s.LastName).Contains(search));
        }

        var projected = students.Select(s => new StudentListItemDto(
            s.Id,
            s.FirstName,
            s.LastName,
            s.ClassId,
            s.Class.Name,
            s.BirthDate,
            s.ParentPhone,
            s.Scores
                .Where(sc => sc.Value != null)
                .Average(sc => sc.Value * 100 / sc.Assessment.MaxScore)));

        var sorted = query.SortBy?.ToLowerInvariant() switch
        {
            "firstname" => projected.OrderBy(s => s.FirstName, query.SortDir).ThenBy(s => s.LastName),
            "classname" => projected.OrderBy(s => s.ClassName, query.SortDir).ThenBy(s => s.LastName).ThenBy(s => s.FirstName),
            "birthdate" => projected.OrderBy(s => s.BirthDate, query.SortDir).ThenBy(s => s.LastName),
            "averagescore" => projected.OrderBy(s => s.AverageScore, query.SortDir).ThenBy(s => s.LastName),
            // Default (and "lastName"): alphabetical, like a class register.
            _ => projected.OrderBy(s => s.LastName, query.SortDir).ThenBy(s => s.FirstName, query.SortDir)
        };

        // Id as the last sort key keeps pages stable when two students have the same name.
        var page = await sorted.ThenBy(s => s.Id).ToPagedResultAsync(query, cancellationToken);

        return page with
        {
            Items = page.Items.Select(s => s with { AverageScore = RoundScore(s.AverageScore) }).ToList()
        };
    }

    public async Task<StudentDto> GetStudentAsync(int id, CancellationToken cancellationToken = default)
    {
        var student = await db.Students
            .AsNoTracking()
            .Where(s => s.Id == id && s.IsActive)
            .Select(s => new StudentDto(
                s.Id, s.FirstName, s.LastName, s.ClassId, s.Class.Name, s.BirthDate, s.ParentPhone, s.Address))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Student", id);

        await classAccess.EnsureCanAccessClassAsync(student.ClassId, cancellationToken);
        return student;
    }

    public async Task<StudentSummaryDto> GetSummaryAsync(int id, CancellationToken cancellationToken = default)
    {
        var student = await GetStudentAsync(id, cancellationToken);

        var scores = db.Scores.AsNoTracking().Where(s => s.StudentId == id && s.Value != null);

        var averageScore = await scores.AverageAsync(s => s.Value * 100 / s.Assessment.MaxScore, cancellationToken);

        var recentScores = await scores
            .OrderByDescending(s => s.Assessment.Date)
            .ThenByDescending(s => s.AssessmentId)
            .Take(RecentScoresCount)
            .Select(s => new RecentScoreDto(
                s.AssessmentId,
                s.Assessment.Title,
                s.Assessment.Subject.Name,
                s.Assessment.Date,
                s.Value!.Value,
                s.Assessment.MaxScore,
                Math.Round(s.Value.Value * 100 / s.Assessment.MaxScore, 1)))
            .ToListAsync(cancellationToken);

        // One small query that counts each status: [{ Present, 40 }, { Absent, 2 }, ...]
        var attendanceByStatus = await db.Attendance
            .AsNoTracking()
            .Where(a => a.StudentId == id)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        int CountOf(AttendanceStatus status) => attendanceByStatus.GetValueOrDefault(status);

        var recordedDays = attendanceByStatus.Values.Sum();
        decimal? attendanceRate = recordedDays == 0
            ? null
            : Math.Round(100m * (CountOf(AttendanceStatus.Present) + CountOf(AttendanceStatus.Late)) / recordedDays, 1);

        return new StudentSummaryDto(
            student,
            RoundScore(averageScore),
            attendanceRate,
            CountOf(AttendanceStatus.Absent),
            CountOf(AttendanceStatus.Late),
            recentScores);
    }

    public async Task<StudentDto> CreateStudentAsync(SaveStudentRequest request, CancellationToken cancellationToken = default)
    {
        var student = new Student { IsActive = true };
        await ApplyAsync(student, request, cancellationToken);

        db.Students.Add(student);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Student {StudentId} created in class {ClassId}", student.Id, student.ClassId);
        return await GetStudentAsync(student.Id, cancellationToken);
    }

    public async Task<StudentDto> UpdateStudentAsync(
        int id,
        SaveStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        var student = await FindActiveStudentAsync(id, cancellationToken);

        await ApplyAsync(student, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Student {StudentId} updated", id);
        return await GetStudentAsync(id, cancellationToken);
    }

    public async Task DeleteStudentAsync(int id, CancellationToken cancellationToken = default)
    {
        var student = await FindActiveStudentAsync(id, cancellationToken);

        student.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Student {StudentId} deactivated", id);
    }

    private async Task<Student> FindActiveStudentAsync(int id, CancellationToken cancellationToken) =>
        await db.Students.SingleOrDefaultAsync(s => s.Id == id && s.IsActive, cancellationToken)
        ?? throw new NotFoundException("Student", id);

    /// <summary>Validates the request against the database and copies it onto the entity.</summary>
    private async Task ApplyAsync(Student student, SaveStudentRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Classes.AnyAsync(c => c.Id == request.ClassId, cancellationToken))
        {
            throw new BusinessRuleException($"Class with id '{request.ClassId}' does not exist.");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (request.BirthDate > today)
        {
            throw new BusinessRuleException("Birth date cannot be in the future.");
        }

        student.FirstName = request.FirstName.Trim();
        student.LastName = request.LastName.Trim();
        student.ClassId = request.ClassId;
        student.BirthDate = request.BirthDate;
        student.ParentPhone = string.IsNullOrWhiteSpace(request.ParentPhone) ? null : request.ParentPhone.Trim();
        student.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
    }

    private static decimal? RoundScore(decimal? score) =>
        score is null ? null : Math.Round(score.Value, 1);
}
