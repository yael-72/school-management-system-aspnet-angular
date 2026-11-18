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

// Both are switched on in appsettings.Development.json and in docker-compose (the demo),
// and stay off in any other environment unless configured explicitly.
if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
{
    // Create/upgrade the database and add demo data on startup.
    await app.Services.InitializeDatabaseAsync();
}

// The Azure deployment copies the Angular build into wwwroot, so one App Service serves both the
// site and the API from the same origin (no CORS). Locally and in Docker there is no wwwroot:
// `ng serve` or nginx serve the site there. WebRootPath is null when the folder does not exist.
var servesWebApp = Directory.Exists(app.Environment.WebRootPath);
if (servesWebApp)
{
    app.UseDefaultFiles();
    app.UseStaticFiles(WebAppFiles.Options);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (servesWebApp)
{
    // Unknown API paths stay a 404 (ProblemDetails); every other path is an Angular route.
    app.MapFallback("/api/{**path}", () => Results.NotFound()).AllowAnonymous();
    app.MapFallbackToFile("index.html", WebAppFiles.Options).AllowAnonymous();
}

// Liveness check for Docker and hosting platforms (no login needed).
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();

// Makes the auto-generated Program class visible to the integration tests (WebApplicationFactory<Program>).
public partial class Program;
