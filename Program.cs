using CourseCore.Api.Modules.Access;
using CourseCore.Api.Modules.AuditLogs;
using CourseCore.Api.Modules.Auth;
using CourseCore.Api.Modules.Courses;
using CourseCore.Api.Modules.Media;
using CourseCore.Api.Modules.Progress;
using CourseCore.Api.Modules.Users;
using CourseCore.Api.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
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
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
