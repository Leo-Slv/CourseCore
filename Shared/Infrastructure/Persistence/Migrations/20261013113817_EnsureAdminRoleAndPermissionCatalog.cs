using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCore.Shared.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// The development seed is the only thing that created the permission catalog and the
    /// Admin role, so a production database (seed disabled) had neither. This makes both
    /// exist everywhere and gives Admin every permission. Idempotent; data only, no schema.
    /// To add a permission later, add a migration like this one (do not rely on the seed).
    /// </summary>
    public partial class EnsureAdminRoleAndPermissionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO permissions (""Id"", ""Key"", ""Name"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
SELECT gen_random_uuid(), v.key, v.name, v.description, now(), now()
FROM (VALUES
    ('users.manage', 'Manage users', 'Manage users'),
    ('roles.manage', 'Manage roles', 'Manage roles'),
    ('areas.manage', 'Manage areas', 'Manage areas'),
    ('courses.manage', 'Manage courses', 'Manage courses'),
    ('videos.manage', 'Manage videos', 'Manage videos'),
    ('progress.read', 'Read progress', 'Read progress'),
    ('audit.read', 'Read audit logs', 'Read audit logs'),
    ('visitors.read', 'Read visitors', 'Read first-time visitor registrations'),
    ('visitors.manage', 'Manage visitors', 'Mark first-time visitors as contacted')
) AS v(key, name, description)
WHERE NOT EXISTS (SELECT 1 FROM permissions p WHERE p.""Key"" = v.key);

INSERT INTO roles (""Id"", ""Name"", ""Description"", ""Active"", ""CreatedAt"", ""UpdatedAt"")
SELECT gen_random_uuid(), 'Admin', 'Administrator role with full access', true, now(), now()
WHERE NOT EXISTS (SELECT 1 FROM roles r WHERE r.""Name"" = 'Admin');

INSERT INTO role_permissions (""RoleId"", ""PermissionId"", ""CreatedAt"")
SELECT r.""Id"", p.""Id"", now()
FROM roles r
CROSS JOIN permissions p
WHERE r.""Name"" = 'Admin'
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionId"" = p.""Id"");
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
