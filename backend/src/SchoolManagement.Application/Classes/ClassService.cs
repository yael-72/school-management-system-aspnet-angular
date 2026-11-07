using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Extensions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Application.Classes;

/// <summary>
/// Reading classes is allowed for every user, limited to the classes she may access
/// (see IClassAccessService). Creating, updating and deleting is for admins only; the controller
/// enforces that with [Authorize(Roles = Admin)].
/// </summary>
public class ClassService(
    IApplicationDbContext db,
    IClassAccessService classAccess,
    ILogger<ClassService> logger) : IClassService
{
    public async Task<PagedResult<ClassSummaryDto>> GetClassesAsync(
        ClassListQuery query,
        CancellationToken cancellationToken = default)
    {
        var classes = db.Classes.AsNoTracking();

        var accessibleClassIds = classAccess.AccessibleClassIds();
        if (accessibleClassIds is not null)
        {
            classes = classes.Where(c => accessibleClassIds.Contains(c.Id));
        }

        if (query.GradeLevel is { } gradeLevel)
        {
            classes = classes.Where(c => c.GradeLevel == gradeLevel);
        }

        if (query.SearchTerm is { } search)
        {
            classes = classes.Where(c =>
                c.Name.Contains(search) ||
                (c.HomeroomTeacher != null && c.HomeroomTeacher.FullName.Contains(search)));
        }

        // Project first, then sort, so we can also sort by computed values (student count, average).
        var projected = classes.Select(c => new ClassSummaryDto(
            c.Id,
            c.Name,
            c.GradeLevel,
            c.SchoolYear,
            c.HomeroomTeacherId,
            c.HomeroomTeacher != null ? c.HomeroomTeacher.FullName : null,
            c.Students.Count(s => s.IsActive),
            c.Students
                .Where(s => s.IsActive)
                .SelectMany(s => s.Scores)
                .Where(s => s.Value != null)
                .Average(s => s.Value * 100 / s.Assessment.MaxScore)));

        var sorted = query.SortBy?.ToLowerInvariant() switch
        {
            "name" => projected.OrderBy(c => c.Name, query.SortDir),
            "studentcount" => projected.OrderBy(c => c.StudentCount, query.SortDir).ThenBy(c => c.Name),
            "averagescore" => projected.OrderBy(c => c.AverageScore, query.SortDir).ThenBy(c => c.Name),
            "homeroomteacher" => projected.OrderBy(c => c.HomeroomTeacherName, query.SortDir).ThenBy(c => c.Name),
            // Default (and "gradeLevel"): youngest grade first, then by name – the natural order of a school.
            _ => projected.OrderBy(c => c.GradeLevel, query.SortDir).ThenBy(c => c.Name)
        };

        var page = await sorted.ToPagedResultAsync(query, cancellationToken);

        // Rounding is done here, in C#, so the SQL stays simple.
        return page with
        {
            Items = page.Items.Select(c => c with { AverageScore = RoundScore(c.AverageScore) }).ToList()
        };
    }

    public async Task<ClassDetailsDto> GetClassAsync(int id, CancellationToken cancellationToken = default)
    {
        var details = await db.Classes
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClassDetailsDto(
                c.Id,
                c.Name,
                c.GradeLevel,
                c.SchoolYear,
                c.HomeroomTeacherId,
                c.HomeroomTeacher != null ? c.HomeroomTeacher.FullName : null,
                c.Students.Count(s => s.IsActive),
                c.Students
                    .Where(s => s.IsActive)
                    .SelectMany(s => s.Scores)
                    .Where(s => s.Value != null)
                    .Average(s => s.Value * 100 / s.Assessment.MaxScore),
                c.TeacherAssignments
                    .OrderBy(a => a.Subject.Name)
                    .Select(a => new ClassSubjectDto(a.SubjectId, a.Subject.Name, a.TeacherId, a.Teacher.FullName))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Class", id);

        await classAccess.EnsureCanAccessClassAsync(id, cancellationToken);

        return details with { AverageScore = RoundScore(details.AverageScore) };
    }

    public async Task<ClassDetailsDto> CreateClassAsync(SaveClassRequest request, CancellationToken cancellationToken = default)
    {
        var schoolClass = new SchoolClass();
        await ApplyAsync(schoolClass, request, cancellationToken);

        db.Classes.Add(schoolClass);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Class {ClassId} ({ClassName}) created", schoolClass.Id, schoolClass.Name);
        return await GetClassAsync(schoolClass.Id, cancellationToken);
    }

    public async Task<ClassDetailsDto> UpdateClassAsync(
        int id,
        SaveClassRequest request,
        CancellationToken cancellationToken = default)
    {
        var schoolClass = await db.Classes.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Class", id);

        await ApplyAsync(schoolClass, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Class {ClassId} updated", id);
        return await GetClassAsync(id, cancellationToken);
    }

    public async Task DeleteClassAsync(int id, CancellationToken cancellationToken = default)
    {
        var schoolClass = await db.Classes.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Class", id);

        var activeStudents = await db.Students.CountAsync(s => s.ClassId == id && s.IsActive, cancellationToken);
        if (activeStudents > 0)
        {
            throw new BusinessRuleException(
                $"Cannot delete a class that still has {activeStudents} active students. Move them to another class first.");
        }

        // Former students' grades and attendance, and the class's exams, are history we must keep.
        // (The database would also refuse: these foreign keys use ON DELETE RESTRICT.)
        var hasHistory = await db.Students.AnyAsync(s => s.ClassId == id, cancellationToken)
            || await db.Assessments.AnyAsync(a => a.ClassId == id, cancellationToken);
        if (hasHistory)
        {
            throw new BusinessRuleException(
                "Cannot delete a class that has former students or assessments, because their history must be kept.");
        }

        db.Classes.Remove(schoolClass);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Class {ClassId} ({ClassName}) deleted", id, schoolClass.Name);
    }

    /// <summary>Validates the request against the database and copies it onto the entity.</summary>
    private async Task ApplyAsync(SchoolClass schoolClass, SaveClassRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        var nameTaken = await db.Classes.AnyAsync(
            c => c.Id != schoolClass.Id && c.Name == name && c.SchoolYear == request.SchoolYear,
            cancellationToken);
        if (nameTaken)
        {
            throw new BusinessRuleException($"A class named '{name}' already exists in school year {request.SchoolYear}.");
        }

        if (request.HomeroomTeacherId is { } teacherId &&
            !await db.Users.AnyAsync(u => u.Id == teacherId && u.IsActive, cancellationToken))
        {
            throw new BusinessRuleException($"Homeroom teacher with id '{teacherId}' does not exist or is not active.");
        }

        schoolClass.Name = name;
        schoolClass.GradeLevel = request.GradeLevel;
        schoolClass.SchoolYear = request.SchoolYear;
        schoolClass.HomeroomTeacherId = request.HomeroomTeacherId;
    }

    private static decimal? RoundScore(decimal? score) =>
        score is null ? null : Math.Round(score.Value, 1);
}
