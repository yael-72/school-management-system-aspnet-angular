using Microsoft.Extensions.Options;
using SchoolManagement.Application.Common.Exceptions;

namespace SchoolManagement.Application.Common.Demo;

/// <summary>
/// In the live demo every visitor logs in with the same published accounts. If one visitor could
/// change their password, deactivate them or change their role, the demo would stop working for
/// everyone else – so in demo mode these changes are refused for the demo accounts.
/// Everything else (classes, grades, attendance, names, files…) stays editable.
/// </summary>
public class DemoAccountGuard(IOptions<DemoOptions> options)
{
    /// <summary>Every seeded account uses this email domain.</summary>
    public const string DemoEmailDomain = "@school.demo";

    public const string Message = "Demo accounts cannot be changed in the live demo.";

    public void EnsureCanChange(string email)
    {
        if (options.Value.Enabled && email.EndsWith(DemoEmailDomain, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(Message);
        }
    }
}
