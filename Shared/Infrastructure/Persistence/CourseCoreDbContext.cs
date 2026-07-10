using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.AuditLogs.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Shared.Infrastructure.Persistence;

public class CourseCoreDbContext : DbContext
{
    public CourseCoreDbContext(DbContextOptions<CourseCoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserPersistenceModel> Users => Set<UserPersistenceModel>();

    public DbSet<RolePersistenceModel> Roles => Set<RolePersistenceModel>();

    public DbSet<PermissionPersistenceModel> Permissions => Set<PermissionPersistenceModel>();

    public DbSet<UserRolePersistenceModel> UserRoles => Set<UserRolePersistenceModel>();

    public DbSet<RolePermissionPersistenceModel> RolePermissions => Set<RolePermissionPersistenceModel>();

    public DbSet<AreaPersistenceModel> Areas => Set<AreaPersistenceModel>();

    public DbSet<UserAreaAccessPersistenceModel> UserAreaAccesses => Set<UserAreaAccessPersistenceModel>();

    public DbSet<RoleAreaAccessPersistenceModel> RoleAreaAccesses => Set<RoleAreaAccessPersistenceModel>();

    public DbSet<CoursePersistenceModel> Courses => Set<CoursePersistenceModel>();

    public DbSet<CourseAreaPersistenceModel> CourseAreas => Set<CourseAreaPersistenceModel>();

    public DbSet<CourseModulePersistenceModel> CourseModules => Set<CourseModulePersistenceModel>();

    public DbSet<LessonPersistenceModel> Lessons => Set<LessonPersistenceModel>();

    public DbSet<VideoPersistenceModel> Videos => Set<VideoPersistenceModel>();

    public DbSet<UserCourseProgressPersistenceModel> UserCourseProgress => Set<UserCourseProgressPersistenceModel>();

    public DbSet<UserLessonProgressPersistenceModel> UserLessonProgress => Set<UserLessonProgressPersistenceModel>();

    public DbSet<AuditLogPersistenceModel> AuditLogs => Set<AuditLogPersistenceModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
