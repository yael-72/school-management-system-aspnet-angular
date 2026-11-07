namespace SchoolManagement.UnitTests.TestHelpers;

/// <summary>A clock that always returns the same time, so time-based results are predictable.</summary>
public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
