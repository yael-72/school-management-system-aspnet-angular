namespace SchoolManagement.Application.Common.Demo;

/// <summary>
/// Settings of the public live demo ("Demo" section). Off by default; switched on only in the
/// Azure deployment, where anyone can log in with the published demo accounts.
/// </summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>
    /// Protects the demo accounts (see <see cref="DemoAccountGuard"/>) and resets the demo data
    /// every time the app starts.
    /// </summary>
    public bool Enabled { get; init; }
}
