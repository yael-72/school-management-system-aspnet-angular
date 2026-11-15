using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Assessments;
using SchoolManagement.Application.Attendance;
using SchoolManagement.Application.Auth;
using SchoolManagement.Application.Classes;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Application.Dashboard;
using SchoolManagement.Application.Events;
using SchoolManagement.Application.Files;
using SchoolManagement.Application.Profile;
using SchoolManagement.Application.Students;
using SchoolManagement.Application.Subjects;
using SchoolManagement.Application.Tasks;
using SchoolManagement.Application.Users;

namespace SchoolManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClassAccessService, ClassAccessService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<ITodoTaskService, TodoTaskService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
