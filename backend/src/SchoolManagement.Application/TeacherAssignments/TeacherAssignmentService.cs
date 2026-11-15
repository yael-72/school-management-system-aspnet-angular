using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Extensions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Application.TeacherAssignments;

public class TeacherAssignmentService(
    IApplicationDbContext db,
    ILogger<TeacherAssignmentService> logger) : ITeacherAssignmentService
{
    public Task<PagedResult<TeacherAssignmentDto>> GetAssignmentsAsync(
        TeacherAssignmentListQuery query,
        CancellationToken cancellationToken = default)
    {
        var assignments = db.TeacherAssignments.AsNoTracking();

        if (query.ClassId is { } classId)
        {
            assignments = assignments.Where(a => a.ClassId == classId);
        }

        if (query.TeacherId is { } teacherId)
        {
            assignments = assignments.Where(a => a.TeacherId == teacherId);
        }

        if (query.SubjectId is { } subjectId)
        {
            assignments = assignments.Where(a => a.SubjectId == subjectId);
        }

        if (query.SearchTerm is { } search)
        {
            assignments = assignments.Where(a =>
                a.Teacher.FullName.Contains(search) || a.Subject.Name.Contains(search) || a.Class.Name.Contains(search));
        }

        var projected = assignments.Select(a => new TeacherAssignmentDto
        {
            Id = a.Id,
            TeacherId = a.TeacherId,
            TeacherName = a.Teacher.FullName,
            TeacherIsActive = a.Teacher.IsActive,
            ClassId = a.ClassId,
            ClassName = a.Class.Name,
            GradeLevel = a.Class.GradeLevel,
            SubjectId = a.SubjectId,
            SubjectName = a.Subject.Name
        });

        var sorted = query.SortBy?.ToLowerInvariant() switch
        {
            "teachername" => projected.OrderBy(a => a.TeacherName, query.SortDir).ThenBy(a => a.GradeLevel).ThenBy(a => a.ClassName),
            "subjectname" => projected.OrderBy(a => a.SubjectName, query.SortDir).ThenBy(a => a.GradeLevel).ThenBy(a => a.ClassName),
            // Default (and "className"): by class, the way a school timetable is read.
            _ => projected.OrderBy(a => a.GradeLevel, query.SortDir).ThenBy(a => a.ClassName, query.SortDir).ThenBy(a => a.SubjectName)
        };

        return sorted.ThenBy(a => a.Id).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<TeacherAssignmentDto> CreateAssignmentAsync(
        CreateTeacherAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        // Any active staff member can teach – an admin can have assignments too.
        if (!await db.Users.AnyAsync(u => u.Id == request.TeacherId && u.IsActive, cancellationToken))
        {
            throw new BusinessRuleException($"Teacher with id '{request.TeacherId}' does not exist or is not active.");
        }

        if (!await db.Classes.AnyAsync(c => c.Id == request.ClassId, cancellationToken))
        {
            throw new BusinessRuleException($"Class with id '{request.ClassId}' does not exist.");
        }

        if (!await db.Subjects.AnyAsync(s => s.Id == request.SubjectId, cancellationToken))
        {
            throw new BusinessRuleException($"Subject with id '{request.SubjectId}' does not exist.");
        }

        // The unique index UQ_TeacherAssignment is the safety net; this gives a clear message first.
        var exists = await db.TeacherAssignments.AnyAsync(
            a => a.TeacherId == request.TeacherId && a.ClassId == request.ClassId && a.SubjectId == request.SubjectId,
            cancellationToken);
        if (exists)
        {
            throw new BusinessRuleException("This teacher already teaches this subject in this class.");
        }

        var assignment = new TeacherAssignment
        {
            TeacherId = request.TeacherId,
            ClassId = request.ClassId,
            SubjectId = request.SubjectId
        };
        db.TeacherAssignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Teacher {TeacherId} assigned to subject {SubjectId} in class {ClassId}",
            request.TeacherId, request.SubjectId, request.ClassId);

        var page = await GetAssignmentsAsync(new TeacherAssignmentListQuery { TeacherId = request.TeacherId, ClassId = request.ClassId, SubjectId = request.SubjectId }, cancellationToken);
        return page.Items.Single();
    }

    public async Task DeleteAssignmentAsync(int id, CancellationToken cancellationToken = default)
    {
        var assignment = await db.TeacherAssignments.SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Teacher assignment", id);

        db.TeacherAssignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Teacher assignment {AssignmentId} removed", id);
    }
}
