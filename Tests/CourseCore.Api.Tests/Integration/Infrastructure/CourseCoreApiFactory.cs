using System.Text;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using CourseCore.Api.Tests.TestDoubles;
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

    private readonly SqliteConnection _connection = new($"Data Source=CourseCoreTests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
    private readonly string _environmentName;
    private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

    public CourseCoreApiFactory()
        : this("Development", null)
    {
    }

    private CourseCoreApiFactory(
        string environmentName,
        IReadOnlyDictionary<string, string?>? configurationOverrides = null)
    {
        _environmentName = environmentName;
        _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
    }

    public static CourseCoreApiFactory Create(
        string environmentName = "Development",
        IReadOnlyDictionary<string, string?>? configurationOverrides = null)
    {
        return new CourseCoreApiFactory(environmentName, configurationOverrides);
    }

    public async Task<Guid> GetAdminRoleIdAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();

        return await dbContext.Roles
            .Where(role => role.Name == AuthRoleNames.Admin)
            .Select(role => role.Id)
            .SingleAsync();
    }

    public async Task SetAdminRoleActiveAsync(bool active)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var adminRole = await dbContext.Roles.SingleAsync(role => role.Name == AuthRoleNames.Admin);

        adminRole.Active = active;
        adminRole.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }

    public async Task<TestUser> SeedUserAsync(string? permissionKey = null)
    {
        return await SeedUserWithRoleAsync(permissionKey);
    }

    public async Task<int> CountActiveRefreshTokensByEmailAsync(string email)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var now = DateTime.UtcNow;

        return await dbContext.RefreshTokens
            .Where(refreshToken => refreshToken.RevokedAt == null
                && refreshToken.ExpiresAt > now
                && dbContext.Users.Any(user => user.Id == refreshToken.UserId && user.Email == email))
            .CountAsync();
    }

    public async Task<int> CountActiveRefreshTokensByUserIdAsync(Guid userId)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var now = DateTime.UtcNow;

        return await dbContext.RefreshTokens
            .Where(refreshToken => refreshToken.UserId == userId
                && refreshToken.RevokedAt == null
                && refreshToken.ExpiresAt > now)
            .CountAsync();
    }

    public async Task<int> GetUserTokenVersionAsync(Guid userId)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();

        return await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => user.TokenVersion)
            .SingleAsync();
    }

    public async Task IncrementUserTokenVersionAsync(Guid userId)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var user = await dbContext.Users.SingleAsync(user => user.Id == userId);

        user.TokenVersion++;
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }

    public async Task<TestUser> SeedUserWithRoleAsync(
        string? permissionKey = null,
        bool roleActive = true,
        string? roleName = null,
        bool userActive = true)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var now = DateTime.UtcNow;
        var user = new UserPersistenceModel
        {
            Id = Guid.NewGuid(),
            Name = "Integration User",
            Email = $"user-{Guid.NewGuid():N}@coursecore.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("IntegrationUser123!"),
            Active = userActive,
            EmailVerifiedAt = now,
            TokenVersion = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Users.Add(user);

        if (!string.IsNullOrWhiteSpace(permissionKey))
        {
            var permission = await dbContext.Permissions.SingleAsync(permission => permission.Key == permissionKey);
            var role = new RolePersistenceModel
            {
                Id = Guid.NewGuid(),
                Name = roleName ?? $"role-{Guid.NewGuid():N}",
                Description = $"Role for {permissionKey}",
                Active = roleActive,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Roles.Add(role);
            dbContext.UserRoles.Add(new UserRolePersistenceModel
            {
                UserId = user.Id,
                RoleId = role.Id,
                CreatedAt = now
            });
            dbContext.RolePermissions.Add(new RolePermissionPersistenceModel
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                CreatedAt = now
            });
        }

        await dbContext.SaveChangesAsync();

        return new TestUser(user.Id, user.Email, "IntegrationUser123!");
    }

    public async Task<Guid> SeedRoleForUserAsync(
        Guid userId,
        bool active = true,
        string? permissionKey = null,
        string? roleName = null)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var now = DateTime.UtcNow;
        var role = new RolePersistenceModel
        {
            Id = Guid.NewGuid(),
            Name = roleName ?? $"role-{Guid.NewGuid():N}",
            Description = "Integration test role",
            Active = active,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Roles.Add(role);
        dbContext.UserRoles.Add(new UserRolePersistenceModel
        {
            UserId = userId,
            RoleId = role.Id,
            CreatedAt = now
        });

        if (!string.IsNullOrWhiteSpace(permissionKey))
        {
            var permission = await dbContext.Permissions.SingleAsync(permission => permission.Key == permissionKey);

            dbContext.RolePermissions.Add(new RolePermissionPersistenceModel
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                CreatedAt = now
            });
        }

        await dbContext.SaveChangesAsync();

        return role.Id;
    }

    public async Task<Guid> SeedAreaAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var area = CreateArea(DateTime.UtcNow);

        dbContext.Areas.Add(area);
        await dbContext.SaveChangesAsync();

        return area.Id;
    }

    public async Task<Guid> SeedPublishedCourseAsync(bool grantAdminAccess)
    {
        var adminUserId = grantAdminAccess ? await GetAdminUserIdAsync() : (Guid?)null;
        var course = await SeedPublishedCourseWithLessonAsync(adminUserId);

        return course.CourseId;
    }

    public async Task<TestCourseData> SeedPublishedCourseWithLessonAsync(
        Guid? grantUserAccess = null,
        CoursePricingModel pricingModel = CoursePricingModel.Paid)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var now = DateTime.UtcNow;
        var area = CreateArea(now);
        var course = new CoursePersistenceModel
        {
            Id = Guid.NewGuid(),
            Title = "Integration Course",
            Slug = $"integration-course-{Guid.NewGuid():N}",
            Description = "Integration test course",
            Published = true,
            DisplayOrder = 0,
            PublishedAt = now,
            PricingModel = pricingModel.ToString(),
            CreatedAt = now,
            UpdatedAt = now
        };
        var module = new CourseModulePersistenceModel
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = "Integration Module",
            Description = "Integration test module",
            Published = true,
            DisplayOrder = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
        var lesson = new LessonPersistenceModel
        {
            Id = Guid.NewGuid(),
            ModuleId = module.Id,
            Title = "Integration Lesson",
            Description = "Integration test lesson",
            FreePreview = false,
            Published = true,
            DisplayOrder = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Areas.Add(area);
        dbContext.Courses.Add(course);
        dbContext.CourseModules.Add(module);
        dbContext.Lessons.Add(lesson);
        dbContext.CourseAreas.Add(new CourseAreaPersistenceModel
        {
            CourseId = course.Id,
            AreaId = area.Id,
            CreatedAt = now
        });

        if (grantUserAccess is not null)
        {
            dbContext.UserAreaAccesses.Add(new UserAreaAccessPersistenceModel
            {
                Id = Guid.NewGuid(),
                UserId = grantUserAccess.Value,
                AreaId = area.Id,
                CanView = true,
                CanManage = false,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await dbContext.SaveChangesAsync();

        return new TestCourseData(area.Id, course.Id, module.Id, lesson.Id);
    }

    public async Task GrantUserAreaAccessAsync(Guid userId, Guid areaId, bool canView = true, bool canManage = false)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var now = DateTime.UtcNow;

        dbContext.UserAreaAccesses.Add(new UserAreaAccessPersistenceModel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AreaId = areaId,
            CanView = canView,
            CanManage = canManage,
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync();
    }

    public async Task GrantRoleAreaAccessAsync(Guid roleId, Guid areaId, bool canView = true, bool canManage = false)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var now = DateTime.UtcNow;

        dbContext.RoleAreaAccesses.Add(new RoleAreaAccessPersistenceModel
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            AreaId = areaId,
            CanView = canView,
            CanManage = canManage,
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync();
    }

    public async Task<TestVideoData> SeedReadyVideoAsync(Guid lessonId, int durationSeconds = 120)
    {
        return await SeedVideoAsync(lessonId, durationSeconds, VideoStatus.Ready);
    }

    public async Task<TestVideoData> SeedVideoAsync(Guid lessonId, int durationSeconds, VideoStatus status)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
        var now = DateTime.UtcNow;
        var video = new VideoPersistenceModel
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            Title = "Integration Video",
            Description = "Integration test video",
            StorageProvider = VideoStorageProvider.Local.ToString(),
            StorageKey = $"videos/{Guid.NewGuid():N}.mp4",
            PlaybackUrl = null,
            DurationSeconds = durationSeconds,
            SizeBytes = 1024,
            Status = status.ToString(),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Videos.Add(video);
        await dbContext.SaveChangesAsync();

        return new TestVideoData(video.Id, video.LessonId);
    }

    public async Task<TestLessonProgressData?> GetLessonProgressAsync(Guid userId, Guid lessonId)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();

        return await dbContext.UserLessonProgress
            .Where(progress => progress.UserId == userId && progress.LessonId == lessonId)
            .Select(progress => new TestLessonProgressData(
                progress.Completed,
                progress.WatchedSeconds,
                progress.CompletedAt))
            .SingleOrDefaultAsync();
    }

    public async Task<TestCourseProgressData?> GetCourseProgressAsync(Guid userId, Guid courseId)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();

        return await dbContext.UserCourseProgress
            .Where(progress => progress.UserId == userId && progress.CourseId == courseId)
            .Select(progress => new TestCourseProgressData(
                progress.ProgressPercent,
                progress.CompletedAt))
            .SingleOrDefaultAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment(_environmentName);
        builder.ConfigureAppConfiguration(configuration =>
        {
            var testConfiguration = new Dictionary<string, string?>
            {
                ["ConnectionStrings:CourseCoreDatabase"] = "Data Source=:memory:",
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SecretKey"] = JwtSecretKey,
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["Auth:ExposeRefreshTokenInBody"] = "false",
                ["Auth:AllowRefreshTokenInBodyFallback"] = "true",
                ["Auth:RefreshTokenCookie:Name"] = "coursecore_refresh_token",
                ["Auth:RefreshTokenCookie:Path"] = "/api/auth",
                ["Auth:RefreshTokenCookie:SameSite"] = "Lax",
                ["Auth:RefreshTokenCookie:Secure"] = "false",
                ["Auth:RefreshTokenCookie:MaxAgeDays"] = "7",
                ["RateLimiting:Login:PermitLimit"] = "100",
                ["RateLimiting:Login:WindowSeconds"] = "60",
                ["RateLimiting:Login:QueueLimit"] = "0",
                ["RateLimiting:Refresh:PermitLimit"] = "100",
                ["RateLimiting:Refresh:WindowSeconds"] = "60",
                ["RateLimiting:Refresh:QueueLimit"] = "0",
                ["RateLimiting:Logout:PermitLimit"] = "100",
                ["RateLimiting:Logout:WindowSeconds"] = "60",
                ["RateLimiting:Logout:QueueLimit"] = "0",
                ["RateLimiting:Register:PermitLimit"] = "100",
                ["RateLimiting:Register:WindowSeconds"] = "60",
                ["RateLimiting:Register:QueueLimit"] = "0",
                ["RateLimiting:ResendConfirmation:PermitLimit"] = "100",
                ["RateLimiting:ResendConfirmation:WindowSeconds"] = "60",
                ["RateLimiting:ResendConfirmation:QueueLimit"] = "0",
                ["Turnstile:SecretKey"] = "integration-test-turnstile-secret",
                ["Resend:ApiKey"] = "integration-test-resend-api-key",
                ["Resend:FromAddress"] = "no-reply@coursecore.local",
                ["Resend:FromName"] = "CourseCore",
                ["Progress:LessonCompletionThresholdPercent"] = "90",
                ["Media:Playback:SignedUrlExpirationMinutes"] = "10",
                ["Media:Playback:SigningSecret"] = "integration-test-media-signing-secret-32-characters-minimum",
                ["Media:Playback:BaseUrl"] = "/media",
                ["Media:Playback:AllowedStorageProviders:0"] = "Local",
                ["Cors:AllowedOrigins:0"] = "https://localhost",
                ["Seed:Admin:Enabled"] = "false"
            };

            foreach (var (key, value) in _configurationOverrides)
            {
                testConfiguration[key] = value;
            }

            configuration.AddInMemoryCollection(testConfiguration);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<CourseCoreDbContext>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<CourseCoreDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CourseCoreDbContext>>();
            services.AddDbContext<CourseCoreDbContext>(options =>
            {
                options.UseSqlite(_connection.ConnectionString);
            });
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidIssuer = JwtIssuer;
                options.TokenValidationParameters.ValidAudience = JwtAudience;
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecretKey));
            });

            services.RemoveAll<ICaptchaVerificationService>();
            services.AddSingleton<ICaptchaVerificationService, FakeCaptchaVerificationService>();
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, FakeEmailSender>();

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
            TokenVersion = 0,
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
        var permissions = new[]
        {
            CreatePermission(AuthPermissionNames.ManageUsers, "Manage users", now),
            CreatePermission(AuthPermissionNames.ManageRoles, "Manage roles", now),
            CreatePermission(AuthPermissionNames.ManageAreas, "Manage areas", now),
            CreatePermission(AuthPermissionNames.ManageCourses, "Manage courses", now),
            CreatePermission(AuthPermissionNames.ManageVideos, "Manage videos", now),
            CreatePermission(AuthPermissionNames.ReadProgress, "Read progress", now),
            CreatePermission(AuthPermissionNames.ReadAudit, "Read audit", now)
        };
        dbContext.Permissions.AddRange(permissions);
        dbContext.UserRoles.Add(new UserRolePersistenceModel
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            CreatedAt = now
        });
        dbContext.RolePermissions.AddRange(permissions.Select(permission => new RolePermissionPersistenceModel
        {
            RoleId = adminRole.Id,
            PermissionId = permission.Id,
            CreatedAt = now
        }));

        dbContext.SaveChanges();
    }

    private static PermissionPersistenceModel CreatePermission(string key, string name, DateTime now)
    {
        return new PermissionPersistenceModel
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = name,
            Description = name,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private async Task<Guid> GetAdminUserIdAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();

        return await dbContext.Users
            .Where(user => user.Email == AdminEmail)
            .Select(user => user.Id)
            .SingleAsync();
    }

    private static AreaPersistenceModel CreateArea(DateTime now)
    {
        return new AreaPersistenceModel
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
    }
}

public sealed record TestUser(Guid Id, string Email, string Password);

public sealed record TestCourseData(Guid AreaId, Guid CourseId, Guid ModuleId, Guid LessonId);

public sealed record TestVideoData(Guid VideoId, Guid LessonId);

public sealed record TestLessonProgressData(bool Completed, int WatchedSeconds, DateTime? CompletedAt);

public sealed record TestCourseProgressData(decimal ProgressPercent, DateTime? CompletedAt);
