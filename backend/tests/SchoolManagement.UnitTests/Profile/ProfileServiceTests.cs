using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Application.Profile;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Profile;

public class ProfileServiceTests
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly FakePasswordHasher _hasher = new();
    private readonly SchoolBuilder _school;

    public ProfileServiceTests() => _school = new SchoolBuilder(_db);

    private ProfileService CreateService(ICurrentUser user, bool demoMode = false) => new(
        _db,
        new ClassAccessService(_db, user),
        user,
        _hasher,
        demoMode ? DemoGuards.On : DemoGuards.Off,
        NullLogger<ProfileService>.Instance);

    [Fact]
    public async Task Teacher_profile_counts_her_classes_students_and_assessments()
    {
        var teacher = await _school.AddTeacherAsync("משה לוינסון");
        var myClass = await _school.AddClassAsync("ז'1", homeroomTeacher: teacher);
        var otherClass = await _school.AddClassAsync("ח'1", 8);
        var subject = await _school.AddSubjectAsync();
        await _school.AddStudentAsync(myClass);
        await _school.AddStudentAsync(myClass, isActive: false);
        await _school.AddStudentAsync(otherClass);
        await _school.AddAssessmentAsync(myClass, subject, teacher);

        // The test database has no roles, so add the one the profile shows.
        _db.Roles.Add(new Domain.Entities.Role { Id = Domain.Constants.RoleIds.Teacher, Name = "Teacher" });
        await _db.SaveChangesAsync();

        var profile = await CreateService(FakeCurrentUser.Teacher(teacher.Id)).GetProfileAsync();

        profile.FullName.Should().Be("משה לוינסון");
        profile.Role.Should().Be("Teacher");
        (profile.ClassCount, profile.StudentCount, profile.AssessmentCount).Should().Be((1, 1, 1));
    }

    [Fact]
    public async Task Update_changes_only_name_and_phone()
    {
        var teacher = await _school.AddTeacherAsync();
        var email = teacher.Email;
        _db.Roles.Add(new Domain.Entities.Role { Id = Domain.Constants.RoleIds.Teacher, Name = "Teacher" });
        await _db.SaveChangesAsync();

        var profile = await CreateService(FakeCurrentUser.Teacher(teacher.Id))
            .UpdateProfileAsync(new UpdateProfileRequest { FullName = "  דוד הירש ", Phone = " " });

        profile.FullName.Should().Be("דוד הירש");
        profile.Phone.Should().BeNull();
        profile.Email.Should().Be(email);
    }

    [Fact]
    public async Task Notification_settings_have_defaults_and_are_created_on_the_first_save()
    {
        var teacher = await _school.AddTeacherAsync();
        var service = CreateService(FakeCurrentUser.Teacher(teacher.Id));

        var defaults = await service.GetNotificationSettingsAsync();
        await service.UpdateNotificationSettingsAsync(defaults with { WeeklySummaryEmail = true, RemindBeforeExam = false });

        defaults.Should().Be(new NotificationSettingsDto { RemindBeforeExam = true, AlertOnLowAttendance = true, WeeklySummaryEmail = false });
        var saved = await _db.NotificationSettings.SingleAsync();
        (saved.UserId, saved.WeeklySummaryEmail, saved.RemindBeforeExam).Should().Be((teacher.Id, true, false));
    }

    [Fact]
    public async Task Password_change_checks_the_current_password_and_stores_the_new_hash()
    {
        var teacher = await _school.AddTeacherAsync();
        teacher.PasswordHash = _hasher.Hash("OldPassword1");
        await _db.SaveChangesAsync();
        var service = CreateService(FakeCurrentUser.Teacher(teacher.Id));

        await service.ChangePasswordAsync(new ChangePasswordRequest { CurrentPassword = "OldPassword1", NewPassword = "NewPassword2" });

        (await _db.Users.SingleAsync(u => u.Id == teacher.Id)).PasswordHash.Should().Be(_hasher.Hash("NewPassword2"));
    }

    [Fact]
    public async Task A_wrong_current_password_is_a_business_rule_error_not_an_authentication_error()
    {
        var teacher = await _school.AddTeacherAsync();
        teacher.PasswordHash = _hasher.Hash("OldPassword1");
        await _db.SaveChangesAsync();

        var act = () => CreateService(FakeCurrentUser.Teacher(teacher.Id))
            .ChangePasswordAsync(new ChangePasswordRequest { CurrentPassword = "Wrong1234", NewPassword = "NewPassword2" });

        // 400 keeps the user logged in; a 401 would make the app log her out.
        await act.Should().ThrowAsync<SchoolManagement.Application.Common.Exceptions.BusinessRuleException>()
            .WithMessage("*current password is incorrect*");
        (await _db.Users.SingleAsync(u => u.Id == teacher.Id)).PasswordHash.Should().Be(_hasher.Hash("OldPassword1"));
    }

    [Fact]
    public async Task The_new_password_must_differ_from_the_current_one()
    {
        var teacher = await _school.AddTeacherAsync();
        teacher.PasswordHash = _hasher.Hash("SamePassword1");
        await _db.SaveChangesAsync();

        var act = () => CreateService(FakeCurrentUser.Teacher(teacher.Id))
            .ChangePasswordAsync(new ChangePasswordRequest { CurrentPassword = "SamePassword1", NewPassword = "SamePassword1" });

        await act.Should().ThrowAsync<SchoolManagement.Application.Common.Exceptions.BusinessRuleException>()
            .WithMessage("*must be different*");
    }

    [Fact]
    public async Task In_the_live_demo_a_demo_account_keeps_its_published_password()
    {
        var teacher = await _school.AddTeacherAsync();
        teacher.Email = "teacher@school.demo";
        teacher.PasswordHash = _hasher.Hash("Demo123!");
        await _db.SaveChangesAsync();

        var act = () => CreateService(FakeCurrentUser.Teacher(teacher.Id), demoMode: true)
            .ChangePasswordAsync(new ChangePasswordRequest { CurrentPassword = "Demo123!", NewPassword = "NewPassword2" });

        await act.Should().ThrowAsync<SchoolManagement.Application.Common.Exceptions.BusinessRuleException>()
            .WithMessage("*live demo*");
        (await _db.Users.SingleAsync(u => u.Id == teacher.Id)).PasswordHash.Should().Be(_hasher.Hash("Demo123!"));
    }
}
