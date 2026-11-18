using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Application.Subjects;

/// <summary>
/// Subjects are school-wide reference data: every logged-in user may read them, only admins change
/// them (the controller enforces that).
/// </summary>
public class SubjectService(IApplicationDbContext db, ILogger<SubjectService> logger) : ISubjectService
{
    public async Task<IReadOnlyList<SubjectDto>> GetSubjectsAsync(CancellationToken cancellationToken = default) =>
        await db.Subjects
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .Select(s => new SubjectDto(s.Id, s.Name))
            .ToListAsync(cancellationToken);

    public async Task<SubjectDto> CreateSubjectAsync(SaveSubjectRequest request, CancellationToken cancellationToken = default)
    {
        var subject = new Subject();
        await ApplyAsync(subject, request, cancellationToken);

        db.Subjects.Add(subject);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Subject {SubjectId} ({Name}) created", subject.Id, subject.Name);
        return new SubjectDto(subject.Id, subject.Name);
    }

    public async Task<SubjectDto> UpdateSubjectAsync(int id, SaveSubjectRequest request, CancellationToken cancellationToken = default)
    {
        var subject = await FindSubjectAsync(id, cancellationToken);
        await ApplyAsync(subject, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Subject {SubjectId} renamed to {Name}", id, subject.Name);
        return new SubjectDto(subject.Id, subject.Name);
    }

    public async Task DeleteSubjectAsync(int id, CancellationToken cancellationToken = default)
    {
        var subject = await FindSubjectAsync(id, cancellationToken);

        // Assessments (with their scores) must be kept; assignments would silently disappear
        // (ON DELETE CASCADE), so both block the delete and the admin removes them on purpose.
        if (await db.Assessments.AnyAsync(a => a.SubjectId == id, cancellationToken))
        {
            throw new BusinessRuleException("Cannot delete a subject that has assessments.");
        }

        if (await db.TeacherAssignments.AnyAsync(a => a.SubjectId == id, cancellationToken))
        {
            throw new BusinessRuleException("Cannot delete a subject that is still taught. Remove its teacher assignments first.");
        }

        db.Subjects.Remove(subject);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Subject {SubjectId} ({Name}) deleted", id, subject.Name);
    }

    private async Task<Subject> FindSubjectAsync(int id, CancellationToken cancellationToken) =>
        await db.Subjects.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
        ?? throw new NotFoundException("Subject", id);

    private async Task ApplyAsync(Subject subject, SaveSubjectRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await db.Subjects.AnyAsync(s => s.Id != subject.Id && s.Name == name, cancellationToken))
        {
            throw new BusinessRuleException($"A subject named '{name}' already exists.");
        }

        subject.Name = name;
    }
}
