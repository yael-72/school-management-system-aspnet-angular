using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Application.Profile;

public class ProfileService(
    IApplicationDbContext db,
    IClassAccessService classAccess,
    ICurrentUser currentUser,
    ILogger<ProfileService> logger) : IProfileService
{
    public async Task<ProfileDto> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.Id;
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.FullName, u.Email, u.Phone, Role = u.Role.Name, u.CreatedAt })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("User", userId);

        // The same class scope as everywhere else: all classes for an admin, her classes for a teacher.
        var classes = db.Classes.AsNoTracking();
        var students = db.Students.AsNoTracking().Where(s => s.IsActive);
        var accessibleClassIds = classAccess.AccessibleClassIds();
        if (accessibleClassIds is not null)
        {
            classes = classes.Where(c => accessibleClassIds.Contains(c.Id));
            students = students.Where(s => accessibleClassIds.Contains(s.ClassId));
        }

        return new ProfileDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Role,
            user.CreatedAt,
            await classes.CountAsync(cancellationToken),
            await students.CountAsync(cancellationToken),
            await db.Assessments.CountAsync(a => a.CreatedByUserId == userId, cancellationToken));
    }

    public async Task<ProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == currentUser.Id, cancellationToken)
            ?? throw new NotFoundException("User", currentUser.Id);

        user.FullName = request.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated the profile", user.Id);
        return await GetProfileAsync(cancellationToken);
    }

    public async Task<NotificationSettingsDto> GetNotificationSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await db.NotificationSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(n => n.UserId == currentUser.Id, cancellationToken);

        // A user without a row yet gets the defaults (the row is created on the first save).
        return ToDto(settings ?? new NotificationSettings());
    }

    public async Task<NotificationSettingsDto> UpdateNotificationSettingsAsync(
        NotificationSettingsDto request,
        CancellationToken cancellationToken = default)
    {
        var settings = await db.NotificationSettings.SingleOrDefaultAsync(n => n.UserId == currentUser.Id, cancellationToken);
        if (settings is null)
        {
            settings = new NotificationSettings { UserId = currentUser.Id };
            db.NotificationSettings.Add(settings);
        }

        settings.RemindBeforeExam = request.RemindBeforeExam;
        settings.AlertOnLowAttendance = request.AlertOnLowAttendance;
        settings.WeeklySummaryEmail = request.WeeklySummaryEmail;
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(settings);
    }

    private static NotificationSettingsDto ToDto(NotificationSettings settings) => new()
    {
        RemindBeforeExam = settings.RemindBeforeExam,
        AlertOnLowAttendance = settings.AlertOnLowAttendance,
        WeeklySummaryEmail = settings.WeeklySummaryEmail
    };
}
