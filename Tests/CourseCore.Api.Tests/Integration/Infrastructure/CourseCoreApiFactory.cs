using System.Text;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace CourseCore.Api.Tests.Integration.Infrastructure;

public sealed class CourseCoreApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin.integration@coursecore.local";
    public const string AdminPassword = "IntegrationAdmin123!";
    private const string JwtIssuer = "CourseCore.IntegrationTests";
    private const string JwtAudience = "CourseCore.IntegrationTests";
    private const string JwtSecretKey = "integration-test-secret-key-32-characters-minimum";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public async Task<Guid> SeedPublishedCourseAsync(bool grantAdminAccess)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var adminUser = await dbContext.Users.SingleAsync(user => user.Email == AdminEmail);
        var now = DateTime.UtcNow;
        var area = new AreaPersistenceModel
        {
            Id = Guid.NewGuid(),
            Name = "Integration Area",
            Slug = $"integration-area-{Guid.NewGuid():N}",
            Description = "Integration test area",
            Active = true,
            DisplayOrder = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
        var course = new CoursePersistenceModel
        {
            Id = Guid.NewGuid(),
            Title = "Integration Course",
            Slug = $"integration-course-{Guid.NewGuid():N}",
            Description = "Integration test course",
            Published = true,
            DisplayOrder = 0,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Areas.Add(area);
        dbContext.Courses.Add(course);
        dbContext.CourseAreas.Add(new CourseAreaPersistenceModel
        {
            CourseId = course.Id,
            AreaId = area.Id,
            CreatedAt = now
        });

        if (grantAdminAccess)
        {
            dbContext.UserAreaAccesses.Add(new UserAreaAccessPersistenceModel
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id,
                AreaId = area.Id,
                CanView = true,
                CanManage = false,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await dbContext.SaveChangesAsync();

        return course.Id;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CourseCoreDatabase"] = "Data Source=:memory:",
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SecretKey"] = JwtSecretKey,
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["Seed:Admin:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<CourseCoreDbContext>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<CourseCoreDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CourseCoreDbContext>>();
            services.AddDbContext<CourseCoreDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidIssuer = JwtIssuer;
                options.TokenValidationParameters.ValidAudience = JwtAudience;
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecretKey));
            });

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();

            dbContext.Database.EnsureCreated();
            SeedAdmin(dbContext);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }

    private static void SeedAdmin(CourseCoreDbContext dbContext)
    {
        if (dbContext.Users.Any(user => user.Email == AdminEmail))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var adminUser = new UserPersistenceModel
        {
            Id = Guid.NewGuid(),
            Name = "Integration Admin",
            Email = AdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
            Active = true,
            EmailVerifiedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        var adminRole = new RolePersistenceModel
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            Description = "Integration test admin role",
            Active = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Users.Add(adminUser);
        dbContext.Roles.Add(adminRole);
        dbContext.UserRoles.Add(new UserRolePersistenceModel
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            CreatedAt = now
        });

        dbContext.SaveChanges();
    }
}
