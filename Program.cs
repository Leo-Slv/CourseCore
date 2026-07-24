using CourseCore.Api.Modules.Access;
using CourseCore.Api.Modules.AuditLogs;
using CourseCore.Api.Modules.Auth;
using CourseCore.Api.Modules.Courses;
using CourseCore.Api.Modules.Media;
using CourseCore.Api.Modules.Progress;
using CourseCore.Api.Modules.Users;
using CourseCore.Api.Shared;
using CourseCore.Api.Shared.Infrastructure.Configuration;
using CourseCore.Api.Shared.Infrastructure.Persistence.Seed;
using CourseCore.Api.Shared.Presentation.Health;
using CourseCore.Api.Shared.Presentation.Middleware;
using CourseCore.Api.Shared.Presentation.OpenApi;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicyName = "CourseCoreCorsPolicy";

if (builder.Environment.IsProduction())
{
    builder.Configuration.ValidateProductionConfiguration();
}

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (builder.Environment.IsDevelopment() && allowedOrigins.Length == 0)
        {
            allowedOrigins =
            [
                "http://localhost:3000",
                "https://localhost:3000"
            ];
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<CourseCoreDbContextHealthCheck>("database", tags: ["ready", "database"]);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddUsersModule();
builder.Services.AddAccessModule();
builder.Services.AddCoursesModule();
builder.Services.AddMediaModule();
builder.Services.AddProgressModule();
builder.Services.AddAuditLogsModule();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("CourseCore API");
    });

    await app.SeedCourseCoreDatabaseAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

app.Run();

public partial class Program
{
}
