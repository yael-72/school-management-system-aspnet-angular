using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Application.TeacherAssignments;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.TeacherAssignments;

public class TeacherAssignmentServiceTests
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly SchoolBuilder _school;
    private readonly TeacherAssignmentService _service;

    public TeacherAssignmentServiceTests()
    {
        _school = new SchoolBuilder(_db);
        _service = new TeacherAssignmentService(_db, NullLogger<TeacherAssignmentService>.Instance);
    }

    [Fact]
    public async Task A_new_assignment_gives_the_teacher_access_to_the_class()
    {
        var teacher = await _school.AddTeacherAsync("משה לוינסון");
        var schoolClass = await _school.AddClassAsync();
        var math = await _school.AddSubjectAsync();
        var access = new ClassAccessService(_db, FakeCurrentUser.Teacher(teacher.Id));
        await access.Invoking(a => a.EnsureCanAccessClassAsync(schoolClass.Id)).Should().ThrowAsync<ForbiddenException>();

        var created = await _service.CreateAssignmentAsync(new CreateTeacherAssignmentRequest
        {
            TeacherId = teacher.Id, ClassId = schoolClass.Id, SubjectId = math.Id
        });

        created.Should().BeEquivalentTo(new { TeacherName = "משה לוינסון", ClassName = "ז'1", SubjectName = "מתמטיקה" });
        await access.Invoking(a => a.EnsureCanGradeAsync(schoolClass.Id, math.Id)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task The_same_assignment_cannot_be_added_twice()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        var math = await _school.AddSubjectAsync();
        await _school.AssignAsync(teacher, schoolClass, math);

        var act = () => _service.CreateAssignmentAsync(new CreateTeacherAssignmentRequest
        {
            TeacherId = teacher.Id, ClassId = schoolClass.Id, SubjectId = math.Id
        });

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*already teaches*");
    }

    [Fact]
    public async Task An_inactive_teacher_cannot_get_a_new_assignment()
    {
        var teacher = await _school.AddTeacherAsync();
        teacher.IsActive = false;
        await _db.SaveChangesAsync();

        var act = async () => await _service.CreateAssignmentAsync(new CreateTeacherAssignmentRequest
        {
            TeacherId = teacher.Id,
            ClassId = (await _school.AddClassAsync()).Id,
            SubjectId = (await _school.AddSubjectAsync()).Id
        });

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*not active*");
    }

    [Fact]
    public async Task List_filters_by_teacher_and_orders_by_class()
    {
        var teacher = await _school.AddTeacherAsync();
        var colleague = await _school.AddTeacherAsync("דוד הירש");
        var seventh = await _school.AddClassAsync("ז'1", 7);
        var eighth = await _school.AddClassAsync("ח'1", 8);
        var math = await _school.AddSubjectAsync();
        await _school.AssignAsync(teacher, eighth, math);
        await _school.AssignAsync(teacher, seventh, math);
        await _school.AssignAsync(colleague, seventh, await _school.AddSubjectAsync("אנגלית"));

        var page = await _service.GetAssignmentsAsync(new TeacherAssignmentListQuery { TeacherId = teacher.Id });

        page.Items.Select(a => a.ClassName).Should().Equal("ז'1", "ח'1");
    }

    [Fact]
    public async Task Removing_an_assignment_deletes_only_that_row()
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        await _school.AssignAsync(teacher, schoolClass, await _school.AddSubjectAsync());
        var id = (await _db.TeacherAssignments.SingleAsync()).Id;

        await _service.DeleteAssignmentAsync(id);

        (await _db.TeacherAssignments.AnyAsync()).Should().BeFalse();
        (await _db.Users.AnyAsync(u => u.Id == teacher.Id)).Should().BeTrue();
    }
}
