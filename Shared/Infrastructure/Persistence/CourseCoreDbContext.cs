using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.AuditLogs.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Certificates.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Questions.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Testimonials.Infrastructure.Persistence.Models;
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

    public DbSet<RefreshTokenPersistenceModel> RefreshTokens => Set<RefreshTokenPersistenceModel>();

    public DbSet<EmailVerificationTokenPersistenceModel> EmailVerificationTokens => Set<EmailVerificationTokenPersistenceModel>();

    public DbSet<PasswordResetTokenPersistenceModel> PasswordResetTokens => Set<PasswordResetTokenPersistenceModel>();

    public DbSet<RolePersistenceModel> Roles => Set<RolePersistenceModel>();

    public DbSet<PermissionPersistenceModel> Permissions => Set<PermissionPersistenceModel>();

    public DbSet<UserRolePersistenceModel> UserRoles => Set<UserRolePersistenceModel>();

    public DbSet<RolePermissionPersistenceModel> RolePermissions => Set<RolePermissionPersistenceModel>();

    public DbSet<AreaPersistenceModel> Areas => Set<AreaPersistenceModel>();

    public DbSet<UserAreaAccessPersistenceModel> UserAreaAccesses => Set<UserAreaAccessPersistenceModel>();

    public DbSet<RoleAreaAccessPersistenceModel> RoleAreaAccesses => Set<RoleAreaAccessPersistenceModel>();

    public DbSet<AccessRequestPersistenceModel> AccessRequests => Set<AccessRequestPersistenceModel>();

    public DbSet<CoursePersistenceModel> Courses => Set<CoursePersistenceModel>();

    public DbSet<CourseAreaPersistenceModel> CourseAreas => Set<CourseAreaPersistenceModel>();

    public DbSet<CourseModulePersistenceModel> CourseModules => Set<CourseModulePersistenceModel>();

    public DbSet<LessonPersistenceModel> Lessons => Set<LessonPersistenceModel>();

    public DbSet<VideoPersistenceModel> Videos => Set<VideoPersistenceModel>();

    public DbSet<LessonMaterialPersistenceModel> LessonMaterials => Set<LessonMaterialPersistenceModel>();

    public DbSet<UserCourseProgressPersistenceModel> UserCourseProgress => Set<UserCourseProgressPersistenceModel>();

    public DbSet<UserLessonProgressPersistenceModel> UserLessonProgress => Set<UserLessonProgressPersistenceModel>();

    public DbSet<LessonNotePersistenceModel> LessonNotes => Set<LessonNotePersistenceModel>();

    public DbSet<LessonQuestionPersistenceModel> LessonQuestions => Set<LessonQuestionPersistenceModel>();

    public DbSet<AuditLogPersistenceModel> AuditLogs => Set<AuditLogPersistenceModel>();

    public DbSet<CertificatePersistenceModel> Certificates => Set<CertificatePersistenceModel>();

    public DbSet<TestimonialPersistenceModel> Testimonials => Set<TestimonialPersistenceModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CourseCoreDbContext).Assembly);
    }
}
