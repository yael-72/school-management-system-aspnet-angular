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
    private readonly SchoolBuilder _school;

    public ProfileServiceTests() => _school = new SchoolBuilder(_db);

    private ProfileService CreateService(ICurrentUser user) =>
        new(_db, new ClassAccessService(_db, user), user, NullLogger<ProfileService>.Instance);

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
}
