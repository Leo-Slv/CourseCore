using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Courses;

public class CourseModuleUseCaseTests
{
    [Fact]
    public async Task CreateCourseModuleUseCase_WhenCourseExists_ShouldAppendModuleWithNextDisplayOrder()
    {
        var courses = new FakeCourseRepository();
        var courseModules = new FakeCourseModuleRepository();
        var course = CreateCourse();
        courses.Courses.Add(course);
        courseModules.Modules.Add(CourseModule.Create(course.Id, "Existing", "Description", 0));
        var auditLogs = new FakeAuditLogService();
        var useCase = new CreateCourseModuleUseCase(courses, courseModules, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(new AddCourseModuleInput
        {
            CourseId = course.Id,
            Title = "New Module",
            Description = "Description"
        });

        Assert.Equal(1, output.DisplayOrder);
        Assert.Contains(courseModules.Modules, module => module.Id == output.Id);
        var auditLog = Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.CourseModuleCreated);
        Assert.Equal("New Module", auditLog.Metadata["displayName"]);
    }

    [Fact]
    public async Task CreateCourseModuleUseCase_WhenCourseDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new CreateCourseModuleUseCase(
            new FakeCourseRepository(), new FakeCourseModuleRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new AddCourseModuleInput
        {
            CourseId = Guid.NewGuid(),
            Title = "Module",
            Description = "Description"
        }));
    }

    [Fact]
    public async Task UpdateCourseModuleUseCase_WhenModuleExists_ShouldUpdateFields()
    {
        var courseModules = new FakeCourseModuleRepository();
        var course = CreateCourse();
        var module = CourseModule.Create(course.Id, "Module", "Description", 0);
        courseModules.Modules.Add(module);
        var auditLogs = new FakeAuditLogService();
        var useCase = new UpdateCourseModuleUseCase(courseModules, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(new UpdateCourseModuleInput
        {
            ModuleId = module.Id,
            Title = "Renamed",
            Description = "New description",
            Published = true
        });

        Assert.Equal("Renamed", output.Title);
        Assert.True(output.Published);
        var auditLog = Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.CourseModuleUpdated);
        Assert.Equal("Renamed", auditLog.Metadata["displayName"]);
    }

    [Fact]
    public async Task RemoveCourseModuleUseCase_WhenModuleHasLessons_ShouldThrowConflictException()
    {
        var courseModules = new FakeCourseModuleRepository();
        var course = CreateCourse();
        var module = CourseModule.Create(course.Id, "Module", "Description", 0);
        module.AddLesson(Lesson.Create(module.Id, "Lesson", "Description", 0));
        courseModules.Modules.Add(module);
        var useCase = new RemoveCourseModuleUseCase(courseModules, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ConflictException>(() => useCase.ExecuteAsync(module.Id));
    }

    [Fact]
    public async Task RemoveCourseModuleUseCase_WhenModuleHasNoLessons_ShouldRemoveModule()
    {
        var courseModules = new FakeCourseModuleRepository();
        var course = CreateCourse();
        var module = CourseModule.Create(course.Id, "Module", "Description", 0);
        courseModules.Modules.Add(module);
        var auditLogs = new FakeAuditLogService();
        var useCase = new RemoveCourseModuleUseCase(courseModules, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(module.Id);

        Assert.DoesNotContain(courseModules.Modules, m => m.Id == module.Id);
        var auditLog = Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.CourseModuleDeleted);
        Assert.Equal("Module", auditLog.Metadata["displayName"]);
    }

    [Fact]
    public async Task ReorderCourseModulesUseCase_WhenIdsMatch_ShouldReorderModules()
    {
        var courses = new FakeCourseRepository();
        var courseModules = new FakeCourseModuleRepository();
        var course = CreateCourse();
        courses.Courses.Add(course);
        var first = CourseModule.Create(course.Id, "First", "Description", 0);
        var second = CourseModule.Create(course.Id, "Second", "Description", 1);
        courseModules.Modules.Add(first);
        courseModules.Modules.Add(second);
        var auditLogs = new FakeAuditLogService();
        var useCase = new ReorderCourseModulesUseCase(courses, courseModules, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(new ReorderCourseModulesInput
        {
            CourseId = course.Id,
            OrderedModuleIds = [second.Id, first.Id]
        });

        Assert.Equal(0, second.DisplayOrder);
        Assert.Equal(1, first.DisplayOrder);
        var auditLog = Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.CourseModuleReordered);
        Assert.Equal(course.Title, auditLog.Metadata["displayName"]);
    }

    [Fact]
    public async Task ReorderCourseModulesUseCase_WhenIdSetDoesNotMatch_ShouldThrowApplicationValidationException()
    {
        var courses = new FakeCourseRepository();
        var courseModules = new FakeCourseModuleRepository();
        var course = CreateCourse();
        courses.Courses.Add(course);
        courseModules.Modules.Add(CourseModule.Create(course.Id, "First", "Description", 0));
        var useCase = new ReorderCourseModulesUseCase(courses, courseModules, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ApplicationValidationException>(() => useCase.ExecuteAsync(new ReorderCourseModulesInput
        {
            CourseId = course.Id,
            OrderedModuleIds = [Guid.NewGuid()]
        }));
    }

    private static Course CreateCourse()
    {
        return Course.Create("Course", Slug.Create($"course-{Guid.NewGuid():N}"), "Description", 0);
    }
}
