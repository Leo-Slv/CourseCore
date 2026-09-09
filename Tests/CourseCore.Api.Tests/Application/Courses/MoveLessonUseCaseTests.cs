using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Application.Validation;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Courses;

public class MoveLessonUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTargetModuleIsValid_ShouldMoveLessonAndAppendDisplayOrder()
    {
        var courseModules = new FakeCourseModuleRepository();
        var lessons = new FakeLessonRepository();
        var auditLogs = new FakeAuditLogService();
        var course = CreateCourse();
        var sourceModule = CourseModule.Create(course.Id, "Source", "Description", 0);
        var targetModule = CourseModule.Create(course.Id, "Target", "Description", 1);
        courseModules.Modules.Add(sourceModule);
        courseModules.Modules.Add(targetModule);
        var existingTargetLesson = Lesson.Create(targetModule.Id, "Existing", "Description", 0);
        lessons.Lessons.Add(existingTargetLesson);
        var lesson = Lesson.Create(sourceModule.Id, "Moving Lesson", "Description", 0);
        lessons.Lessons.Add(lesson);
        var useCase = new MoveLessonUseCase(lessons, courseModules, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(new MoveLessonInput
        {
            LessonId = lesson.Id,
            TargetModuleId = targetModule.Id
        });

        Assert.Equal(targetModule.Id, output.ModuleId);
        Assert.Equal(1, output.DisplayOrder);
        var auditLog = Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.LessonMoved);
        Assert.Equal(sourceModule.Id.ToString(), auditLog.Metadata["fromModuleId"]);
        Assert.Equal(targetModule.Id.ToString(), auditLog.Metadata["toModuleId"]);
        Assert.Equal("Moving Lesson", auditLog.Metadata["displayName"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTargetModuleIsSameAsCurrent_ShouldBeNoOp()
    {
        var courseModules = new FakeCourseModuleRepository();
        var lessons = new FakeLessonRepository();
        var auditLogs = new FakeAuditLogService();
        var course = CreateCourse();
        var module = CourseModule.Create(course.Id, "Module", "Description", 0);
        courseModules.Modules.Add(module);
        var lesson = Lesson.Create(module.Id, "Lesson", "Description", 0);
        lessons.Lessons.Add(lesson);
        var useCase = new MoveLessonUseCase(lessons, courseModules, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(new MoveLessonInput
        {
            LessonId = lesson.Id,
            TargetModuleId = module.Id
        });

        Assert.Equal(module.Id, output.ModuleId);
        Assert.Empty(auditLogs.Entries);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLessonDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new MoveLessonUseCase(
            new FakeLessonRepository(), new FakeCourseModuleRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new MoveLessonInput
        {
            LessonId = Guid.NewGuid(),
            TargetModuleId = Guid.NewGuid()
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTargetModuleDoesNotExist_ShouldThrowNotFoundException()
    {
        var courseModules = new FakeCourseModuleRepository();
        var lessons = new FakeLessonRepository();
        var course = CreateCourse();
        var module = CourseModule.Create(course.Id, "Module", "Description", 0);
        courseModules.Modules.Add(module);
        var lesson = Lesson.Create(module.Id, "Lesson", "Description", 0);
        lessons.Lessons.Add(lesson);
        var useCase = new MoveLessonUseCase(lessons, courseModules, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new MoveLessonInput
        {
            LessonId = lesson.Id,
            TargetModuleId = Guid.NewGuid()
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTargetModuleBelongsToDifferentCourse_ShouldThrowApplicationValidationException()
    {
        var courseModules = new FakeCourseModuleRepository();
        var lessons = new FakeLessonRepository();
        var sourceCourse = CreateCourse();
        var otherCourse = CreateCourse();
        var sourceModule = CourseModule.Create(sourceCourse.Id, "Source", "Description", 0);
        var targetModule = CourseModule.Create(otherCourse.Id, "Target", "Description", 0);
        courseModules.Modules.Add(sourceModule);
        courseModules.Modules.Add(targetModule);
        var lesson = Lesson.Create(sourceModule.Id, "Lesson", "Description", 0);
        lessons.Lessons.Add(lesson);
        var useCase = new MoveLessonUseCase(lessons, courseModules, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ApplicationValidationException>(() => useCase.ExecuteAsync(new MoveLessonInput
        {
            LessonId = lesson.Id,
            TargetModuleId = targetModule.Id
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTargetModuleIsFull_ShouldThrowConflictException()
    {
        var courseModules = new FakeCourseModuleRepository();
        var lessons = new FakeLessonRepository();
        var course = CreateCourse();
        var sourceModule = CourseModule.Create(course.Id, "Source", "Description", 0);
        var targetModule = CourseModule.Create(course.Id, "Target", "Description", 1);
        courseModules.Modules.Add(sourceModule);
        courseModules.Modules.Add(targetModule);
        for (var i = 0; i < CourseValidationLimits.MaxLessonsPerModule; i++)
        {
            lessons.Lessons.Add(Lesson.Create(targetModule.Id, $"Lesson {i}", "Description", i));
        }
        var lesson = Lesson.Create(sourceModule.Id, "Lesson", "Description", 0);
        lessons.Lessons.Add(lesson);
        var useCase = new MoveLessonUseCase(lessons, courseModules, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ConflictException>(() => useCase.ExecuteAsync(new MoveLessonInput
        {
            LessonId = lesson.Id,
            TargetModuleId = targetModule.Id
        }));
    }

    private static Course CreateCourse()
    {
        return Course.Create("Course", Slug.Create($"course-{Guid.NewGuid():N}"), "Description", 0);
    }
}
