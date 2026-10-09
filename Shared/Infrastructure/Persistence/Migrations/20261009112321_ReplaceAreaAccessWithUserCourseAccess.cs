using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourseCore.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAreaAccessWithUserCourseAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_course_accesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_course_accesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_course_accesses_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_course_accesses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_course_accesses_CourseId",
                table: "user_course_accesses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_user_course_accesses_UserId",
                table: "user_course_accesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_course_accesses_UserId_CourseId",
                table: "user_course_accesses",
                columns: new[] { "UserId", "CourseId" },
                unique: true);

            // Preserve existing access: every still-valid per-user area grant becomes a per-course grant
            // for the non-free courses linked to that area (Source = 2, Migrated).
            // Role-based area access is intentionally not migrated; staff access now comes from the
            // Admin role / courses.manage permission.
            migrationBuilder.Sql(@"
INSERT INTO user_course_accesses (""Id"", ""UserId"", ""CourseId"", ""Source"", ""CreatedAt"", ""UpdatedAt"")
SELECT gen_random_uuid(), grants.""UserId"", grants.""CourseId"", 2, now(), now()
FROM (
    SELECT DISTINCT uaa.""UserId"", ca.""CourseId""
    FROM user_area_accesses uaa
    INNER JOIN course_areas ca ON ca.""AreaId"" = uaa.""AreaId""
    INNER JOIN courses c ON c.""Id"" = ca.""CourseId""
    WHERE (uaa.""CanView"" OR uaa.""CanManage"")
      AND (uaa.""StartsAt"" IS NULL OR uaa.""StartsAt"" <= now())
      AND (uaa.""ExpiresAt"" IS NULL OR uaa.""ExpiresAt"" >= now())
      AND c.""PricingModel"" <> 'Free'
) AS grants;");

            migrationBuilder.DropTable(
                name: "role_area_accesses");

            migrationBuilder.DropTable(
                name: "user_area_accesses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_course_accesses");

            migrationBuilder.CreateTable(
                name: "role_area_accesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanManage = table.Column<bool>(type: "boolean", nullable: false),
                    CanView = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_area_accesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_area_accesses_areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_role_area_accesses_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_area_accesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanManage = table.Column<bool>(type: "boolean", nullable: false),
                    CanView = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_area_accesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_area_accesses_areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_area_accesses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_role_area_accesses_AreaId",
                table: "role_area_accesses",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_role_area_accesses_RoleId",
                table: "role_area_accesses",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_role_area_accesses_RoleId_AreaId",
                table: "role_area_accesses",
                columns: new[] { "RoleId", "AreaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_area_accesses_AreaId",
                table: "user_area_accesses",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_user_area_accesses_UserId",
                table: "user_area_accesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_area_accesses_UserId_AreaId",
                table: "user_area_accesses",
                columns: new[] { "UserId", "AreaId" },
                unique: true);
        }
    }
}
