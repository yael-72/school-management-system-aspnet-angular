using System.Security.Cryptography;
using System.Text.Json.Serialization;
using SchoolManagement.Api.Extensions;
using SchoolManagement.Api.Infrastructure;
using SchoolManagement.Application;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Infrastructure;
using SchoolManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// In development, if no JWT signing key was configured (user-secrets / environment variable),
// use a random one so the app runs with zero setup. Tokens then stop working after a restart.
// In any other environment a missing key stops the app at startup (see JwtOptions validation).
if (builder.Environment.IsDevelopment() && string.IsNullOrEmpty(builder.Configuration["Jwt:SigningKey"]))
{
    builder.Configuration["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddJwtAuthentication(builder.Configuration);
// Enums travel as names ("Present") instead of numbers (0), so the API is readable and
// reordering an enum can never silently change the meaning of stored client data.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Create/upgrade the database and add demo data on startup (development only).
    await app.Services.InitializeDatabaseAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Makes the auto-generated Program class visible to the integration tests (WebApplicationFactory<Program>).
public partial class Program;
