using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Application.Auth;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Application.Students;
using SchoolManagement.Application.Users;

namespace SchoolManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClassAccessService, ClassAccessService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
