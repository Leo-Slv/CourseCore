using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.UseCases;
using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Application.UseCases;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.AuditLogs;

public class SensitiveActionAuditTests
{
    [Fact]
    public async Task CreateUser_ShouldRecordUserCreatedAuditLog()
    {
        var users = new FakeUserRepository();
        var auditLogs = new FakeAuditLogService();
        var useCase = new CreateUserUseCase(users, new FakePasswordHasher(), new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(new CreateUserInput
        {
            Name = "New User",
            Email = "new.user@coursecore.local",
            Password = "password"
        });

        var auditLog = Assert.Single(auditLogs.Entries);
        Assert.Equal(AuditLogActionNames.UserCreated, auditLog.Action);
        Assert.Equal("User", auditLog.EntityName);
        Assert.Equal(output.Id, auditLog.EntityId);
    }

    [Fact]
    public async Task UpdateUser_ShouldRecordUserUpdatedAuditLog()
    {
        var user = TestEntityFactory.User();
        var users = new FakeUserRepository();
        users.Add(user);
        var auditLogs = new FakeAuditLogService();
        var useCase = new UpdateUserUseCase(users, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(new UpdateUserInput
        {
            UserId = user.Id,
            Name = "Updated User",
            Email = "updated.user@coursecore.local",
            Active = true
        });

        var auditLog = Assert.Single(auditLogs.Entries);
        Assert.Equal(AuditLogActionNames.UserUpdated, auditLog.Action);
        Assert.Equal("User", auditLog.EntityName);
        Assert.Equal(user.Id, auditLog.EntityId);
    }

    [Fact]
    public async Task GrantUserAreaAccess_ShouldRecordUserAreaAccessGrantedAuditLog()
    {
        var user = TestEntityFactory.User();
        var area = TestEntityFactory.Area();
        var users = new FakeUserRepository();
        var areas = new FakeAreaRepository();
        users.Add(user);
        areas.Areas.Add(area);
        var auditLogs = new FakeAuditLogService();
        var useCase = new GrantUserAreaAccessUseCase(users, areas, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(new GrantUserAreaAccessInput
        {
            UserId = user.Id,
            AreaId = area.Id,
            CanView = true,
            CanManage = false
        });

        var access = Assert.Single(areas.UserAreaAccesses);
        var auditLog = Assert.Single(auditLogs.Entries);
        Assert.Equal(AuditLogActionNames.UserAreaAccessGranted, auditLog.Action);
        Assert.Equal("UserAreaAccess", auditLog.EntityName);
        Assert.Equal(access.Id, auditLog.EntityId);
        Assert.Equal(user.Id.ToString(), auditLog.Metadata["targetUserId"]);
        Assert.Equal(area.Id.ToString(), auditLog.Metadata["areaId"]);
    }

    [Fact]
    public async Task GrantRoleAreaAccess_ShouldRecordRoleAreaAccessGrantedAuditLog()
    {
        var role = TestEntityFactory.Role();
        var area = TestEntityFactory.Area();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        roles.AddForUser(Guid.NewGuid(), role);
        areas.Areas.Add(area);
        var auditLogs = new FakeAuditLogService();
        var useCase = new GrantRoleAreaAccessUseCase(roles, areas, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(new GrantRoleAreaAccessInput
        {
            RoleId = role.Id,
            AreaId = area.Id,
            CanView = true,
            CanManage = true
        });

        var access = Assert.Single(areas.RoleAreaAccesses);
        var auditLog = Assert.Single(auditLogs.Entries);
        Assert.Equal(AuditLogActionNames.RoleAreaAccessGranted, auditLog.Action);
        Assert.Equal("RoleAreaAccess", auditLog.EntityName);
        Assert.Equal(access.Id, auditLog.EntityId);
        Assert.Equal(role.Id.ToString(), auditLog.Metadata["roleId"]);
        Assert.Equal(area.Id.ToString(), auditLog.Metadata["areaId"]);
    }

    [Fact]
    public async Task CreateCourse_ShouldRecordCourseCreatedAuditLog()
    {
        var courses = new FakeCourseRepository();
        var auditLogs = new FakeAuditLogService();
        var useCase = new CreateCourseUseCase(courses, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(new CreateCourseInput
        {
            Title = "Course",
            Slug = $"course-{Guid.NewGuid():N}",
            Description = "Course description",
            DisplayOrder = 1
        });

        var auditLog = Assert.Single(auditLogs.Entries);
        Assert.Equal(AuditLogActionNames.CourseCreated, auditLog.Action);
        Assert.Equal("Course", auditLog.EntityName);
        Assert.Equal(output.Id, auditLog.EntityId);
    }

    [Fact]
    public async Task UpdateCourse_ShouldRecordCourseUpdatedAuditLog()
    {
        var course = Course.Create(
            "Course",
            Slug.Create($"course-{Guid.NewGuid():N}"),
            "Course description",
            displayOrder: 1);
        var courses = new FakeCourseRepository();
        courses.Courses.Add(course);
        var auditLogs = new FakeAuditLogService();
        var useCase = new UpdateCourseUseCase(courses, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(new UpdateCourseInput
        {
            CourseId = course.Id,
            Title = "Updated Course",
            Slug = $"updated-course-{Guid.NewGuid():N}",
            Description = "Updated description",
            DisplayOrder = 2
        });

        var auditLog = Assert.Single(auditLogs.Entries);
        Assert.Equal(AuditLogActionNames.CourseUpdated, auditLog.Action);
        Assert.Equal("Course", auditLog.EntityName);
        Assert.Equal(course.Id, auditLog.EntityId);
    }

    [Fact]
    public async Task PublishCourse_ShouldRecordCoursePublishedAuditLog()
    {
        var course = Course.Create(
            "Course",
            Slug.Create($"course-{Guid.NewGuid():N}"),
            "Course description",
            displayOrder: 1);
        var courses = new FakeCourseRepository();
        courses.Courses.Add(course);
        var auditLogs = new FakeAuditLogService();
        var useCase = new PublishCourseUseCase(courses, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(new PublishCourseInput { CourseId = course.Id });

        var auditLog = Assert.Single(auditLogs.Entries);
        Assert.Equal(AuditLogActionNames.CoursePublished, auditLog.Action);
        Assert.Equal("Course", auditLog.EntityName);
        Assert.Equal(course.Id, auditLog.EntityId);
    }

    [Fact]
    public async Task CreateVideo_ShouldRecordVideoCreatedAuditLog()
    {
        var lesson = Lesson.Create(Guid.NewGuid(), "Lesson", "Lesson description", displayOrder: 1);
        var videos = new FakeVideoRepository();
        var lessons = new FakeLessonRepository();
        lessons.Lessons.Add(lesson);
        var auditLogs = new FakeAuditLogService();
        var useCase = new CreateVideoUseCase(videos, lessons, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(new CreateVideoInput
        {
            LessonId = lesson.Id,
            Title = "Video",
            Description = "Video description",
            StorageProvider = "Local",
            StorageKey = "videos/video.mp4",
            DurationSeconds = 120,
            SizeBytes = 1024
        });

        var auditLog = Assert.Single(auditLogs.Entries);
        Assert.Equal(AuditLogActionNames.VideoCreated, auditLog.Action);
        Assert.Equal("Video", auditLog.EntityName);
        Assert.Equal(output.Id, auditLog.EntityId);
        Assert.Equal(lesson.Id.ToString(), auditLog.Metadata["lessonId"]);
        Assert.DoesNotContain("storage", string.Join(',', auditLog.Metadata.Keys), StringComparison.OrdinalIgnoreCase);
    }
}
