using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Application.Events;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Events;

public class EventServiceTests
{
    // An id no test teacher gets, so "created by the admin" is never "created by the teacher".
    private static readonly FakeCurrentUser Admin = FakeCurrentUser.Admin(id: 999);

    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly SchoolBuilder _school;

    public EventServiceTests() => _school = new SchoolBuilder(_db);

    private EventService CreateService(ICurrentUser user) =>
        new(_db, new ClassAccessService(_db, user), user, NullLogger<EventService>.Instance);

    private static SaveEventRequest Event(string title, DateOnly start, DateOnly? end = null, int? classId = null) => new()
    {
        Title = title,
        StartDate = start,
        EndDate = end,
        ClassId = classId
    };

    private static EventQuery November => new() { From = new DateOnly(2026, 11, 1), To = new DateOnly(2026, 11, 30) };

    [Fact]
    public async Task Events_that_overlap_the_range_are_returned_by_start_date()
    {
        var admin = CreateService(Admin);
        await admin.CreateEventAsync(Event("טיול שנתי", new DateOnly(2026, 10, 30), new DateOnly(2026, 11, 1)));
        await admin.CreateEventAsync(Event("אסיפת הורים", new DateOnly(2026, 11, 12)));
        await admin.CreateEventAsync(Event("חנוכה", new DateOnly(2026, 12, 4)));

        var events = await admin.GetEventsAsync(November);

        events.Select(e => e.Title).Should().Equal("טיול שנתי", "אסיפת הורים");
    }

    [Fact]
    public async Task Teacher_sees_school_wide_events_and_events_of_her_classes_only()
    {
        var teacher = await _school.AddTeacherAsync();
        var myClass = await _school.AddClassAsync("ז'1", homeroomTeacher: teacher);
        var otherClass = await _school.AddClassAsync("ח'1", 8);
        var admin = CreateService(Admin);
        await admin.CreateEventAsync(Event("ישיבת צוות", new DateOnly(2026, 11, 3)));
        await admin.CreateEventAsync(Event("טיול ז'1", new DateOnly(2026, 11, 4), classId: myClass.Id));
        await admin.CreateEventAsync(Event("טיול ח'1", new DateOnly(2026, 11, 5), classId: otherClass.Id));

        var events = await CreateService(FakeCurrentUser.Teacher(teacher.Id)).GetEventsAsync(November);

        events.Select(e => e.Title).Should().Equal("ישיבת צוות", "טיול ז'1");
        events.Should().OnlyContain(e => !e.CanEdit, "the admin created them");
    }

    [Fact]
    public async Task Teacher_can_add_an_event_to_her_class_but_not_a_school_wide_one()
    {
        var teacher = await _school.AddTeacherAsync();
        var myClass = await _school.AddClassAsync(homeroomTeacher: teacher);
        var service = CreateService(FakeCurrentUser.Teacher(teacher.Id));

        var created = await service.CreateEventAsync(Event("מבחן מתכונת", new DateOnly(2026, 11, 20), classId: myClass.Id));

        created.CanEdit.Should().BeTrue();
        await service.Invoking(s => s.CreateEventAsync(Event("יום ספורט", new DateOnly(2026, 11, 21))))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Teacher_cannot_change_an_event_she_did_not_create()
    {
        var teacher = await _school.AddTeacherAsync();
        var myClass = await _school.AddClassAsync(homeroomTeacher: teacher);
        var adminEvent = await CreateService(Admin)
            .CreateEventAsync(Event("טיול", new DateOnly(2026, 11, 4), classId: myClass.Id));

        var act = () => CreateService(FakeCurrentUser.Teacher(teacher.Id)).DeleteEventAsync(adminEvent.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task End_date_before_start_date_is_rejected()
    {
        var act = () => CreateService(Admin)
            .CreateEventAsync(Event("טיול", new DateOnly(2026, 11, 4), end: new DateOnly(2026, 11, 3)));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*end date*");
    }
}
