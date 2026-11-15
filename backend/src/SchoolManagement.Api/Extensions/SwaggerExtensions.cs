using Microsoft.OpenApi;

namespace SchoolManagement.Api.Extensions;

public static class SwaggerExtensions
{
    private const string BearerScheme = "Bearer";

    /// <summary>
    /// Swagger UI with an "Authorize" button: paste the token from POST /api/auth/login
    /// and Swagger sends it as "Authorization: Bearer ..." on every request.
    /// </summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "School Management API",
                Version = "v1",
                Description = "REST API for managing classes, students, attendance, exams and grades."
            });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the accessToken returned by POST /api/auth/login."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, document)] = []
            });
        });

        return services;
    }
}
