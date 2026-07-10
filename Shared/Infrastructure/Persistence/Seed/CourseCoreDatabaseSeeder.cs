using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Shared.Infrastructure.Persistence.Seed;

public sealed class CourseCoreDatabaseSeeder
{
    private static readonly SeedPermission[] Permissions =
    [
        new("users.manage", "Manage users", "Manage users"),
        new("roles.manage", "Manage roles", "Manage roles"),
        new("areas.manage", "Manage areas", "Manage areas"),
        new("courses.manage", "Manage courses", "Manage courses"),
        new("videos.manage", "Manage videos", "Manage videos"),
        new("progress.read", "Read progress", "Read progress"),
        new("audit.read", "Read audit logs", "Read audit logs")
    ];

    private static readonly SeedArea[] Areas =
    [
        new("admin", "Administration", "Administration", 1),
        new("courses", "Courses", "Courses", 2),
        new("media", "Media", "Media", 3),
        new("progress", "Progress", "Progress", 4)
    ];

    private readonly CourseCoreDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AdminSeedOptions _options;
    private readonly ILogger<CourseCoreDatabaseSeeder> _logger;

    public CourseCoreDatabaseSeeder(
        CourseCoreDbContext dbContext,
        IPasswordHasher passwordHasher,
        IOptions<AdminSeedOptions> options,
        ILogger<CourseCoreDatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("CourseCore database seed is disabled.");
            return;
        }

        var adminName = ValidateName(_options.Name);
        var adminEmail = ValidateEmail(_options.Email);
        var adminPassword = ValidatePassword(_options.Password);
        var now = DateTime.UtcNow;

        var adminRole = await EnsureAdminRoleAsync(now, cancellationToken);
        var permissions = await EnsurePermissionsAsync(now, cancellationToken);
        var areas = await EnsureAreasAsync(now, cancellationToken);
        var adminUser = await EnsureAdminUserAsync(adminName, adminEmail, adminPassword, now, cancellationToken);

        EnsureAdminUserRole(adminUser.Id, adminRole.Id, now);
        EnsureAdminRolePermissions(adminRole.Id, permissions, now);
        EnsureAdminRoleAreaAccesses(adminRole.Id, areas, now);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CourseCore database seed completed.");
    }

    private async Task<RolePersistenceModel> EnsureAdminRoleAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles.FirstOrDefaultAsync(
            item => item.Name == "Admin",
            cancellationToken);

        if (role is null)
        {
            role = new RolePersistenceModel
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                Description = "Administrator role with full access",
                Active = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Roles.Add(role);
            return role;
        }

        role.Description = "Administrator role with full access";
        role.Active = true;
        role.UpdatedAt = now;

        return role;
    }

    private async Task<IReadOnlyCollection<PermissionPersistenceModel>> EnsurePermissionsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var keys = Permissions.Select(permission => permission.Key).ToArray();
        var existingPermissions = await _dbContext.Permissions
            .Where(permission => keys.Contains(permission.Key))
            .ToListAsync(cancellationToken);

        foreach (var seedPermission in Permissions)
        {
            var permission = existingPermissions.FirstOrDefault(item => item.Key == seedPermission.Key);

            if (permission is null)
            {
                permission = new PermissionPersistenceModel
                {
                    Id = Guid.NewGuid(),
                    Key = seedPermission.Key,
                    CreatedAt = now
                };

                existingPermissions.Add(permission);
                _dbContext.Permissions.Add(permission);
            }

            permission.Name = seedPermission.Name;
            permission.Description = seedPermission.Description;
            permission.UpdatedAt = now;
        }

        return existingPermissions;
    }

    private async Task<IReadOnlyCollection<AreaPersistenceModel>> EnsureAreasAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var slugs = Areas.Select(area => area.Slug).ToArray();
        var existingAreas = await _dbContext.Areas
            .Where(area => slugs.Contains(area.Slug))
            .ToListAsync(cancellationToken);

        foreach (var seedArea in Areas)
        {
            var area = existingAreas.FirstOrDefault(item => item.Slug == seedArea.Slug);

            if (area is null)
            {
                area = new AreaPersistenceModel
                {
                    Id = Guid.NewGuid(),
                    Slug = seedArea.Slug,
                    CreatedAt = now
                };

                existingAreas.Add(area);
                _dbContext.Areas.Add(area);
            }

            area.Name = seedArea.Name;
            area.Description = seedArea.Description;
            area.Active = true;
            area.DisplayOrder = seedArea.DisplayOrder;
            area.UpdatedAt = now;
        }

        return existingAreas;
    }

    private async Task<UserPersistenceModel> EnsureAdminUserAsync(
        string name,
        string email,
        string password,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var adminUser = await _dbContext.Users.FirstOrDefaultAsync(
            user => user.Email == email,
            cancellationToken);

        if (adminUser is null)
        {
            adminUser = new UserPersistenceModel
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                PasswordHash = _passwordHasher.Hash(password),
                Active = true,
                EmailVerifiedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Users.Add(adminUser);
            return adminUser;
        }

        adminUser.Name = name;
        adminUser.Active = true;
        adminUser.EmailVerifiedAt ??= now;
        adminUser.UpdatedAt = now;

        if (_options.ResetPassword)
        {
            adminUser.PasswordHash = _passwordHasher.Hash(password);
        }

        return adminUser;
    }

    private void EnsureAdminUserRole(Guid userId, Guid roleId, DateTime now)
    {
        var exists = _dbContext.UserRoles.Local.Any(item => item.UserId == userId && item.RoleId == roleId) ||
            _dbContext.UserRoles.Any(item => item.UserId == userId && item.RoleId == roleId);

        if (exists)
        {
            return;
        }

        _dbContext.UserRoles.Add(new UserRolePersistenceModel
        {
            UserId = userId,
            RoleId = roleId,
            CreatedAt = now
        });
    }

    private void EnsureAdminRolePermissions(
        Guid roleId,
        IReadOnlyCollection<PermissionPersistenceModel> permissions,
        DateTime now)
    {
        foreach (var permission in permissions)
        {
            var exists = _dbContext.RolePermissions.Local.Any(
                    item => item.RoleId == roleId && item.PermissionId == permission.Id) ||
                _dbContext.RolePermissions.Any(
                    item => item.RoleId == roleId && item.PermissionId == permission.Id);

            if (exists)
            {
                continue;
            }

            _dbContext.RolePermissions.Add(new RolePermissionPersistenceModel
            {
                RoleId = roleId,
                PermissionId = permission.Id,
                CreatedAt = now
            });
        }
    }

    private void EnsureAdminRoleAreaAccesses(
        Guid roleId,
        IReadOnlyCollection<AreaPersistenceModel> areas,
        DateTime now)
    {
        foreach (var area in areas)
        {
            var access = _dbContext.RoleAreaAccesses.Local.FirstOrDefault(
                    item => item.RoleId == roleId && item.AreaId == area.Id) ??
                _dbContext.RoleAreaAccesses.FirstOrDefault(
                    item => item.RoleId == roleId && item.AreaId == area.Id);

            if (access is null)
            {
                _dbContext.RoleAreaAccesses.Add(new RoleAreaAccessPersistenceModel
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    AreaId = area.Id,
                    CanView = true,
                    CanManage = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });

                continue;
            }

            access.CanView = true;
            access.CanManage = true;
            access.UpdatedAt = now;
        }
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Seed admin name is required.");
        }

        return name.Trim();
    }

    private static string ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Seed admin email is required.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!normalizedEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Seed admin email is invalid.");
        }

        return normalizedEmail;
    }

    private static string ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Seed admin password is required when Seed:Admin:Enabled is true.");
        }

        return password;
    }

    private sealed record SeedPermission(string Key, string Name, string Description);

    private sealed record SeedArea(string Slug, string Name, string Description, int DisplayOrder);
}
