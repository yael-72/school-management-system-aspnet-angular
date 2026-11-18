using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Extensions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Models;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Application.Users;

/// <summary>Staff management (admin only – the controller enforces the role).</summary>
public class UserService(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<UserService> logger) : IUserService
{
    public Task<PagedResult<UserListItemDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            users = users.Where(u => u.Role.Name == query.Role);
        }

        if (query.IsActive is { } isActive)
        {
            users = users.Where(u => u.IsActive == isActive);
        }

        if (query.SearchTerm is { } search)
        {
            users = users.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
        }

        var projected = users.Select(u => new UserListItemDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role.Name,
            Phone = u.Phone,
            IsActive = u.IsActive,
            AssignmentCount = u.TeacherAssignments.Count()
        });

        var sorted = query.SortBy?.ToLowerInvariant() switch
        {
            "email" => projected.OrderBy(u => u.Email, query.SortDir),
            "role" => projected.OrderBy(u => u.Role, query.SortDir).ThenBy(u => u.FullName),
            "assignmentcount" => projected.OrderBy(u => u.AssignmentCount, query.SortDir).ThenBy(u => u.FullName),
            _ => projected.OrderBy(u => u.FullName, query.SortDir)
        };

        return sorted.ThenBy(u => u.Id).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<UserDto> GetUserAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto(u.Id, u.FullName, u.Email, u.Phone, u.RoleId, u.Role.Name, u.IsActive, u.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("User", id);

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            PasswordHash = passwordHasher.Hash(request.Password),
            // Every user gets a settings row with the defaults, like the demo users.
            NotificationSettings = new NotificationSettings()
        };
        await ApplyAsync(user, request, cancellationToken);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} created by admin {AdminId}", user.Id, currentUser.Id);
        return await GetUserAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(id, cancellationToken);

        if (id == currentUser.Id && request.RoleId != user.RoleId)
        {
            // Otherwise the last admin could remove her own access to user management.
            throw new BusinessRuleException("You cannot change your own role.");
        }

        await ApplyAsync(user, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated by admin {AdminId}", id, currentUser.Id);
        return await GetUserAsync(id, cancellationToken);
    }

    public async Task<UserDto> SetStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(id, cancellationToken);

        if (id == currentUser.Id && !isActive)
        {
            throw new BusinessRuleException("You cannot deactivate your own account.");
        }

        user.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} {Status} by admin {AdminId}", id, isActive ? "activated" : "deactivated", currentUser.Id);
        return await GetUserAsync(id, cancellationToken);
    }

    public async Task DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(id, cancellationToken);

        if (id == currentUser.Id)
        {
            throw new BusinessRuleException("You cannot delete your own account.");
        }

        // Grades, attendance, classes and events point to the user who created them; that history
        // must stay, so such a user is deactivated instead. (The database would refuse anyway:
        // these foreign keys use ON DELETE RESTRICT.)
        var hasHistory =
            await db.TeacherAssignments.AnyAsync(a => a.TeacherId == id, cancellationToken)
            || await db.Classes.AnyAsync(c => c.HomeroomTeacherId == id, cancellationToken)
            || await db.Assessments.AnyAsync(a => a.CreatedByUserId == id, cancellationToken)
            || await db.Attendance.AnyAsync(a => a.RecordedByUserId == id, cancellationToken)
            || await db.Events.AnyAsync(e => e.CreatedByUserId == id, cancellationToken);
        if (hasHistory)
        {
            throw new BusinessRuleException(
                "This user has classes, assignments or school records and cannot be deleted. Deactivate the user instead.");
        }

        // Personal data (to-do list, files, notification settings) is removed with the user (ON DELETE CASCADE).
        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} deleted by admin {AdminId}", id, currentUser.Id);
    }

    public async Task ResetPasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(id, cancellationToken);

        user.PasswordHash = passwordHasher.Hash(newPassword);
        await db.SaveChangesAsync(cancellationToken);

        // Never log the password itself – only who reset whose.
        logger.LogInformation("Password of user {UserId} reset by admin {AdminId}", id, currentUser.Id);
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        await db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new RoleDto(r.Id, r.Name))
            .ToListAsync(cancellationToken);

    private async Task<User> FindUserAsync(int id, CancellationToken cancellationToken) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
        ?? throw new NotFoundException("User", id);

    /// <summary>Validates the request against the database and copies it onto the entity.</summary>
    private async Task ApplyAsync(User user, SaveUserRequestBase request, CancellationToken cancellationToken)
    {
        // Emails are stored in lower case (the login compares them that way).
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Id != user.Id && u.Email == email, cancellationToken))
        {
            throw new BusinessRuleException($"A user with the email '{email}' already exists.");
        }

        if (!await db.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken))
        {
            throw new BusinessRuleException($"Role with id '{request.RoleId}' does not exist.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.RoleId = request.RoleId;
    }
}
