using SchoolManagement.Infrastructure;
using SchoolManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Create/upgrade the database and add demo data on startup (development only).
    await app.Services.InitializeDatabaseAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Makes the auto-generated Program class visible to the integration tests (WebApplicationFactory<Program>).
public partial class Program;
