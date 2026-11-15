using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Extensions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Application.Assessments;

/// <summary>
/// Exams, quizzes and assignments, and the scores students got on them.
///
/// Permissions (see IClassAccessService): anyone who may access a class can read its assessments and
/// scores. Creating, editing, deleting and grading need permission for the assessment's subject in
/// that class – admins always, teachers only for a subject they teach there.
/// Inactive students are left out of grade sheets and statistics.
/// </summary>
public class AssessmentService(
    IApplicationDbContext db,
    IClassAccessService classAccess,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<AssessmentService> logger) : IAssessmentService
{
    public async Task<PagedResult<AssessmentListItemDto>> GetAssessmentsAsync(
        AssessmentListQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.From > query.To)
        {
            throw new BusinessRuleException("The start date must be on or before the end date.");
        }

        var assessments = db.Assessments.AsNoTracking();

        if (query.ClassId is { } classId)
        {
            // A teacher asking for a class she does not teach gets 403, not a silently empty list.
            await classAccess.EnsureCanAccessClassAsync(classId, cancellationToken);
            assessments = assessments.Where(a => a.ClassId == classId);
        }

        var accessibleClassIds = classAccess.AccessibleClassIds();
        if (accessibleClassIds is not null)
        {
            assessments = assessments.Where(a => accessibleClassIds.Contains(a.ClassId));
        }

        if (query.SubjectId is { } subjectId)
        {
            assessments = assessments.Where(a => a.SubjectId == subjectId);
        }

        if (query.Type is { } type)
        {
            assessments = assessments.Where(a => a.Type == type);
        }

        if (query.From is { } from)
        {
            assessments = assessments.Where(a => a.Date >= from);
        }

        if (query.To is { } to)
        {
            assessments = assessments.Where(a => a.Date <= to);
        }

        if (query.SearchTerm is { } search)
        {
            assessments = assessments.Where(a => a.Title.Contains(search) || a.Subject.Name.Contains(search));
        }

        var projected = assessments.Select(a => new AssessmentListItemDto
        {
            Id = a.Id,
            Title = a.Title,
            Type = a.Type,
            ClassId = a.ClassId,
            ClassName = a.Class.Name,
            SubjectId = a.SubjectId,
            SubjectName = a.Subject.Name,
            Date = a.Date,
            MaxScore = a.MaxScore,
            GradedCount = a.Scores.Count(s => s.Value != null && s.Student.IsActive),
            StudentCount = a.Class.Students.Count(s => s.IsActive),
            AverageScore = a.Scores
                .Where(s => s.Value != null && s.Student.IsActive)
                .Average(s => s.Value * 100 / a.MaxScore)
        });

        var sorted = query.SortBy?.ToLowerInvariant() switch
        {
            "date" => projected.OrderBy(a => a.Date, query.SortDir),
            "title" => projected.OrderBy(a => a.Title, query.SortDir),
            "classname" => projected.OrderBy(a => a.ClassName, query.SortDir).ThenByDescending(a => a.Date),
            "subjectname" => projected.OrderBy(a => a.SubjectName, query.SortDir).ThenByDescending(a => a.Date),
            "averagescore" => projected.OrderBy(a => a.AverageScore, query.SortDir).ThenByDescending(a => a.Date),
            // Default: newest first, which is what a teacher looks for most of the time.
            _ => projected.OrderByDescending(a => a.Date)
        };

        // Id as the last sort key keeps pages stable when two assessments share a date.
        var page = await sorted.ThenBy(a => a.Id).ToPagedResultAsync(query, cancellationToken);

        // One small query for the user's subjects, instead of a sub-query per row.
        var gradable = await classAccess.GetGradableSubjectsAsync(cancellationToken);

        return page with
        {
            Items = page.Items
                .Select(a => a with
                {
                    AverageScore = RoundPercent(a.AverageScore),
                    CanEdit = CanGrade(gradable, a.ClassId, a.SubjectId)
                })
                .ToList()
        };
    }

    public async Task<AssessmentDto> GetAssessmentAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await db.Assessments
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Type,
                a.ClassId,
                ClassName = a.Class.Name,
                a.SubjectId,
                SubjectName = a.Subject.Name,
                a.Date,
                a.MaxScore,
                CreatedByName = a.CreatedByUser.FullName
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Assessment", id);

        await classAccess.EnsureCanAccessClassAsync(row.ClassId, cancellationToken);
        var gradable = await classAccess.GetGradableSubjectsAsync(cancellationToken);

        return new AssessmentDto(
            row.Id, row.Title, row.Type, row.ClassId, row.ClassName, row.SubjectId, row.SubjectName,
            row.Date, row.MaxScore, row.CreatedByName, CanGrade(gradable, row.ClassId, row.SubjectId));
    }

    public async Task<AssessmentDto> CreateAssessmentAsync(
        SaveAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureClassAndSubjectExistAsync(request, cancellationToken);
        await classAccess.EnsureCanGradeAsync(request.ClassId, request.SubjectId, cancellationToken);

        var assessment = new Assessment
        {
            CreatedByUserId = currentUser.Id,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        Apply(assessment, request);

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Assessment {AssessmentId} created for class {ClassId}, subject {SubjectId}",
            assessment.Id, assessment.ClassId, assessment.SubjectId);
        return await GetAssessmentAsync(assessment.Id, cancellationToken);
    }

    public async Task<AssessmentDto> UpdateAssessmentAsync(
        int id,
        SaveAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var assessment = await FindAssessmentAsync(id, cancellationToken);

        // She must be allowed to edit it where it is now, and where she wants to move it.
        await classAccess.EnsureCanGradeAsync(assessment.ClassId, assessment.SubjectId, cancellationToken);
        await EnsureClassAndSubjectExistAsync(request, cancellationToken);
        await classAccess.EnsureCanGradeAsync(request.ClassId, request.SubjectId, cancellationToken);

        if (request.ClassId != assessment.ClassId && await db.Scores.AnyAsync(s => s.AssessmentId == id, cancellationToken))
        {
            throw new BusinessRuleException("An assessment that already has scores cannot be moved to another class.");
        }

        var highestScore = await db.Scores
            .Where(s => s.AssessmentId == id)
            .MaxAsync(s => s.Value, cancellationToken);
        if (highestScore > request.MaxScore)
        {
            throw new BusinessRuleException(
                $"Max score cannot be lower than an existing score ({highestScore:0.##}).");
        }

        Apply(assessment, request);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Assessment {AssessmentId} updated", id);
        return await GetAssessmentAsync(id, cancellationToken);
    }

    public async Task DeleteAssessmentAsync(int id, CancellationToken cancellationToken = default)
    {
        var assessment = await FindAssessmentAsync(id, cancellationToken);
        await classAccess.EnsureCanGradeAsync(assessment.ClassId, assessment.SubjectId, cancellationToken);

        // The scores are deleted with it (ON DELETE CASCADE on Scores.AssessmentId). Loading them
        // lets EF Core remove them too, so the same happens on providers without cascades.
        await db.Scores.Where(s => s.AssessmentId == id).LoadAsync(cancellationToken);
        db.Assessments.Remove(assessment);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Assessment {AssessmentId} ({Title}) deleted", id, assessment.Title);
    }

    public async Task<AssessmentScoresDto> GetScoresAsync(int id, CancellationToken cancellationToken = default)
    {
        var assessment = await GetAssessmentAsync(id, cancellationToken);

        // Each active student with their score on this assessment, if there is one (a LEFT JOIN in SQL).
        var rows = await db.Students
            .AsNoTracking()
            .Where(s => s.ClassId == assessment.ClassId && s.IsActive)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ThenBy(s => s.Id)
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                Score = s.Scores
                    .Where(sc => sc.AssessmentId == id)
                    .Select(sc => new { sc.Value, sc.Comment })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var students = rows
            .Select(r => new StudentScoreDto(
                r.Id,
                r.FirstName,
                r.LastName,
                r.Score?.Value,
                ToPercent(r.Score?.Value, assessment.MaxScore),
                r.Score?.Comment))
            .ToList();

        var graded = students.Where(s => s.Score is not null).Select(s => s.Score!.Value).ToList();

        return new AssessmentScoresDto(
            assessment,
            graded.Count,
            graded.Count == 0 ? null : RoundPercent(graded.Average() * 100 / assessment.MaxScore),
            graded.Count == 0 ? null : graded.Max(),
            graded.Count == 0 ? null : graded.Min(),
            students);
    }

    public async Task<AssessmentScoresDto> SaveScoresAsync(
        int id,
        SaveScoresRequest request,
        CancellationToken cancellationToken = default)
    {
        var assessment = await FindAssessmentAsync(id, cancellationToken);
        await classAccess.EnsureCanGradeAsync(assessment.ClassId, assessment.SubjectId, cancellationToken);

        if (assessment.Date > Today())
        {
            throw new BusinessRuleException("Scores cannot be entered before the assessment date.");
        }

        var studentIds = request.Scores.Select(s => s.StudentId).ToList();
        if (studentIds.Distinct().Count() != studentIds.Count)
        {
            throw new BusinessRuleException("Each student can appear only once in a scores request.");
        }

        foreach (var entry in request.Scores)
        {
            // Business rule: a score is between 0 and the assessment's max score.
            if (entry.Score < 0 || entry.Score > assessment.MaxScore)
            {
                throw new BusinessRuleException(
                    $"Score of student {entry.StudentId} must be between 0 and the max score ({assessment.MaxScore:0.##}).");
            }

            if (entry.Score is { } score && score != Math.Round(score, 2))
            {
                throw new BusinessRuleException("Scores can have at most 2 decimal places.");
            }
        }

        var activeStudentIds = await db.Students
            .Where(s => s.ClassId == assessment.ClassId && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var unknownStudentIds = studentIds.Except(activeStudentIds).ToList();
        if (unknownStudentIds.Count > 0)
        {
            throw new BusinessRuleException(
                $"Students {string.Join(", ", unknownStudentIds)} are not active students of this class.");
        }

        // Upsert: update the scores that exist and add the missing ones.
        // The unique index on (AssessmentId, StudentId) is the safety net against duplicates.
        var existingScores = await db.Scores
            .Where(s => s.AssessmentId == id && studentIds.Contains(s.StudentId))
            .ToDictionaryAsync(s => s.StudentId, cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var entry in request.Scores)
        {
            var comment = string.IsNullOrWhiteSpace(entry.Comment) ? null : entry.Comment.Trim();
            existingScores.TryGetValue(entry.StudentId, out var score);

            // Nothing to keep: an empty row clears the student's score (or is simply skipped).
            if (entry.Score is null && comment is null)
            {
                if (score is not null)
                {
                    db.Scores.Remove(score);
                }
                continue;
            }

            if (score is null)
            {
                score = new Score { AssessmentId = id, StudentId = entry.StudentId };
                db.Scores.Add(score);
            }

            score.Value = entry.Score;
            score.Comment = comment;
            score.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Scores of assessment {AssessmentId} saved by user {UserId} ({Count} rows)",
            id, currentUser.Id, request.Scores.Count);

        return await GetScoresAsync(id, cancellationToken);
    }

    private async Task<Assessment> FindAssessmentAsync(int id, CancellationToken cancellationToken) =>
        await db.Assessments.SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
        ?? throw new NotFoundException("Assessment", id);

    private async Task EnsureClassAndSubjectExistAsync(SaveAssessmentRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Classes.AnyAsync(c => c.Id == request.ClassId, cancellationToken))
        {
            throw new BusinessRuleException($"Class with id '{request.ClassId}' does not exist.");
        }

        if (!await db.Subjects.AnyAsync(s => s.Id == request.SubjectId, cancellationToken))
        {
            throw new BusinessRuleException($"Subject with id '{request.SubjectId}' does not exist.");
        }
    }

    private static void Apply(Assessment assessment, SaveAssessmentRequest request)
    {
        assessment.Title = request.Title.Trim();
        assessment.Type = request.Type!.Value;
        assessment.ClassId = request.ClassId;
        assessment.SubjectId = request.SubjectId;
        assessment.Date = request.Date!.Value;
        assessment.MaxScore = request.MaxScore;
    }

    private static bool CanGrade(IReadOnlySet<ClassSubject>? gradable, int classId, int subjectId) =>
        gradable is null || gradable.Contains(new ClassSubject(classId, subjectId));

    private static decimal? ToPercent(decimal? score, decimal maxScore) =>
        score is null ? null : RoundPercent(score.Value * 100 / maxScore);

    private static decimal? RoundPercent(decimal? percent) =>
        percent is null ? null : Math.Round(percent.Value, 1);

    // Same convention as the rest of the API: dates are compared in UTC.
    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
