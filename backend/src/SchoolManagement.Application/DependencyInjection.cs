using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Auth;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Application.Students;

namespace SchoolManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClassAccessService, ClassAccessService>();
        services.AddScoped<IStudentService, StudentService>();
        return services;
    }
}
