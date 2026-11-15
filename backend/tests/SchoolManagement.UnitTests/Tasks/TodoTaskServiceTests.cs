using FluentAssertions;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Tasks;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Tasks;

public class TodoTaskServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly AppDbContext _db = TestDbContextFactory.Create();

    private TodoTaskService ServiceFor(int userId) =>
        new(_db, FakeCurrentUser.Teacher(userId), new FixedTimeProvider(Now));

    private static SaveTodoTaskRequest Task(string title, int? dueDay = null) => new()
    {
        Title = title,
        DueDate = dueDay is { } day ? new DateOnly(2026, 11, day) : null
    };

    [Fact]
    public async Task Tasks_are_personal_and_listed_open_first_by_due_date()
    {
        var mine = ServiceFor(1);
        var done = await mine.CreateTaskAsync(Task("הושלם", dueDay: 1));
        await mine.CreateTaskAsync(Task("בלי תאריך"));
        await mine.CreateTaskAsync(Task("מחר", dueDay: 11));
        await mine.CreateTaskAsync(Task("היום", dueDay: 10));
        await mine.ToggleTaskAsync(done.Id);
        await ServiceFor(2).CreateTaskAsync(Task("של מורה אחר"));

        var tasks = await mine.GetTasksAsync();

        tasks.Select(t => t.Title).Should().Equal("היום", "מחר", "בלי תאריך", "הושלם");
    }

    [Fact]
    public async Task Toggle_flips_the_done_flag()
    {
        var service = ServiceFor(1);
        var task = await service.CreateTaskAsync(Task("  לבדוק מבחנים  "));

        var toggled = await service.ToggleTaskAsync(task.Id);

        toggled.IsDone.Should().BeTrue();
        toggled.Title.Should().Be("לבדוק מבחנים");
        (await service.ToggleTaskAsync(task.Id)).IsDone.Should().BeFalse();
    }

    [Fact]
    public async Task Another_users_task_is_not_found()
    {
        var task = await ServiceFor(1).CreateTaskAsync(Task("פרטי"));
        var stranger = ServiceFor(2);

        await stranger.Invoking(s => s.DeleteTaskAsync(task.Id)).Should().ThrowAsync<NotFoundException>();
        await stranger.Invoking(s => s.ToggleTaskAsync(task.Id)).Should().ThrowAsync<NotFoundException>();
    }
}
