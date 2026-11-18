using Microsoft.Extensions.Options;
using SchoolManagement.Application.Common.Demo;

namespace SchoolManagement.UnitTests.TestHelpers;

public static class DemoGuards
{
    /// <summary>The normal case: demo mode off, nothing is protected.</summary>
    public static DemoAccountGuard Off { get; } = new(Options.Create(new DemoOptions()));

    /// <summary>The live demo: accounts with an @school.demo email are protected.</summary>
    public static DemoAccountGuard On { get; } = new(Options.Create(new DemoOptions { Enabled = true }));
}
