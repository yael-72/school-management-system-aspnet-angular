using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Users;
using SchoolManagement.Domain.Constants;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Users;

public class UserServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly SchoolBuilder _school;
    private readonly User _admin;

    public UserServiceTests()
    {
        _school = new SchoolBuilder(_db);
        _db.Roles.AddRange(
            new Role { Id = RoleIds.Admin, Name = RoleNames.Admin },
            new Role { Id = RoleIds.Teacher, Name = RoleNames.Teacher });
        _admin = new User { FullName = "אהרן אברמסון", Email = "admin@school.test", PasswordHash = "x", RoleId = RoleIds.Admin, IsActive = true };
        _db.Users.Add(_admin);
        _db.SaveChanges();
    }

    private UserService CreateService(ICurrentUser? user = null) => new(
        _db, new FakePasswordHasher(), user ?? FakeCurrentUser.Admin(_admin.Id), new FixedTimeProvider(Now), NullLogger<UserService>.Instance);

    private static CreateUserRequest NewTeacher(string email = "David.Hirsch@School.Test") => new()
    {
        FullName = "  דוד הירש ",
        Email = email,
        Phone = "050-1234567",
        RoleId = RoleIds.Teacher,
        Password = "Teacher123"
    };

    [Fact]
    public async Task Create_hashes_the_password_and_stores_the_email_in_lower_case()
    {
        var created = await CreateService().CreateUserAsync(NewTeacher());

        created.Should().BeEquivalentTo(new { FullName = "דוד הירש", Email = "david.hirsch@school.test", Role = RoleNames.Teacher, IsActive = true });
        var user = await _db.Users.Include(u => u.NotificationSettings).SingleAsync(u => u.Id == created.Id);
        user.PasswordHash.Should().Be("hashed:Teacher123");
        user.NotificationSettings.Should().NotBeNull();
    }

    [Fact]
    public async Task Email_must_be_unique_ignoring_case()
    {
        var act = () => CreateService().CreateUserAsync(NewTeacher("ADMIN@school.test"));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task Admin_cannot_deactivate_delete_or_demote_herself()
    {
        var service = CreateService();

        await service.Invoking(s => s.SetStatusAsync(_admin.Id, false)).Should().ThrowAsync<BusinessRuleException>().WithMessage("*own account*");
        await service.Invoking(s => s.DeleteUserAsync(_admin.Id)).Should().ThrowAsync<BusinessRuleException>().WithMessage("*own account*");
        await service.Invoking(s => s.UpdateUserAsync(_admin.Id, new UpdateUserRequest
        {
            FullName = _admin.FullName, Email = _admin.Email, RoleId = RoleIds.Teacher
        })).Should().ThrowAsync<BusinessRuleException>().WithMessage("*own role*");
    }

    [Fact]
    public async Task A_user_with_school_history_can_be_deactivated_but_not_deleted()
    {
        var teacher = await _school.AddTeacherAsync();
        await _school.AddClassAsync(homeroomTeacher: teacher);
        var service = CreateService();

        await service.Invoking(s => s.DeleteUserAsync(teacher.Id))
            .Should().ThrowAsync<BusinessRuleException>().WithMessage("*Deactivate the user instead*");

        var deactivated = await service.SetStatusAsync(teacher.Id, false);
        deactivated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task A_user_without_history_is_deleted()
    {
        var service = CreateService();
        var created = await service.CreateUserAsync(NewTeacher());

        await service.DeleteUserAsync(created.Id);

        (await _db.Users.AnyAsync(u => u.Id == created.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task List_shows_how_many_assignments_each_user_has()
    {
        var teacher = await _school.AddTeacherAsync("משה לוינסון");
        await _school.AssignAsync(teacher, await _school.AddClassAsync(), await _school.AddSubjectAsync());

        var page = await CreateService().GetUsersAsync(new UserListQuery { Role = RoleNames.Teacher });

        page.Items.Should().ContainSingle().Which.AssignmentCount.Should().Be(1);
    }
}
