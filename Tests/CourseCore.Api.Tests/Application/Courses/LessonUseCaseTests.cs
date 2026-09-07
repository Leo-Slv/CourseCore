using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Modules.Progress.Domain.Entities;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Courses;

public class LessonUseCaseTests
{
    [Fact]
    public async Task CreateLessonUseCase_WhenModuleExists_ShouldAppendLessonWithNextDisplayOrder()
    {
        var courseModules = new FakeCourseModuleRepository();
        var lessons = new FakeLessonRepository();
        var module = CreateModule();
        module.AddLesson(Lesson.Create(module.Id, "Existing", "Description", 0));
        courseModules.Modules.Add(module);
        var useCase = new CreateLessonUseCase(courseModules, lessons, new FakeUnitOfWork(), new FakeAuditLogService());

        var output = await useCase.ExecuteAsync(new AddLessonInput
        {
            ModuleId = module.Id,
            Title = "New Lesson",
            Description = "Description",
            FreePreview = true
        });

        Assert.Equal(1, output.DisplayOrder);
        Assert.True(output.FreePreview);
        Assert.Contains(lessons.Lessons, lesson => lesson.Id == output.Id);
    }

    [Fact]
    public async Task CreateLessonUseCase_WhenModuleDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new CreateLessonUseCase(
            new FakeCourseModuleRepository(), new FakeLessonRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new AddLessonInput
        {
            ModuleId = Guid.NewGuid(),
            Title = "Lesson",
            Description = "Description"
        }));
    }

    [Fact]
    public async Task UpdateLessonUseCase_WhenLessonExists_ShouldUpdateFields()
    {
        var lessons = new FakeLessonRepository();
        var module = CreateModule();
        var lesson = Lesson.Create(module.Id, "Lesson", "Description", 0);
        lessons.Lessons.Add(lesson);
        var useCase = new UpdateLessonUseCase(lessons, new FakeUnitOfWork(), new FakeAuditLogService());

        var output = await useCase.ExecuteAsync(new UpdateLessonInput
        {
            LessonId = lesson.Id,
            Title = "Renamed",
            Description = "New description",
            FreePreview = true,
            Published = true
        });

        Assert.Equal("Renamed", output.Title);
        Assert.True(output.FreePreview);
        Assert.True(output.Published);
    }

    [Fact]
    public async Task RemoveLessonUseCase_WhenLessonHasProgress_ShouldThrowConflictException()
    {
        var lessons = new FakeLessonRepository();
        var videos = new FakeVideoRepository();
        var progress = new FakeProgressRepository();
        var module = CreateModule();
        var lesson = Lesson.Create(module.Id, "Lesson", "Description", 0);
        lessons.Lessons.Add(lesson);
        await progress.SaveLessonProgressAsync(UserLessonProgress.Create(Guid.NewGuid(), lesson.Id));
        var useCase = new RemoveLessonUseCase(lessons, videos, progress, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ConflictException>(() => useCase.ExecuteAsync(lesson.Id));
    }

    [Fact]
    public async Task RemoveLessonUseCase_WhenLessonHasVideoAndNoProgress_ShouldRemoveVideoAndLesson()
    {
        var lessons = new FakeLessonRepository();
        var videos = new FakeVideoRepository();
        var progress = new FakeProgressRepository();
        var module = CreateModule();
        var lesson = Lesson.Create(module.Id, "Lesson", "Description", 0);
        lessons.Lessons.Add(lesson);
        var video = Video.Create(lesson.Id, "Video", "Description", VideoStorageProvider.YouTube, "abc123XYZ_-", 100, 0);
        videos.Videos.Add(video);
        var useCase = new RemoveLessonUseCase(lessons, videos, progress, new FakeUnitOfWork(), new FakeAuditLogService());

        await useCase.ExecuteAsync(lesson.Id);

        Assert.DoesNotContain(lessons.Lessons, l => l.Id == lesson.Id);
        Assert.DoesNotContain(videos.Videos, v => v.Id == video.Id);
    }

    [Fact]
    public async Task ReorderLessonsUseCase_WhenIdsMatch_ShouldReorderLessons()
    {
        var courseModules = new FakeCourseModuleRepository();
        var lessons = new FakeLessonRepository();
        var module = CreateModule();
        var first = Lesson.Create(module.Id, "First", "Description", 0);
        var second = Lesson.Create(module.Id, "Second", "Description", 1);
        module.AddLesson(first);
        module.AddLesson(second);
        courseModules.Modules.Add(module);
        lessons.Lessons.Add(first);
        lessons.Lessons.Add(second);
        var useCase = new ReorderLessonsUseCase(courseModules, lessons, new FakeUnitOfWork(), new FakeAuditLogService());

        await useCase.ExecuteAsync(new ReorderLessonsInput
        {
            ModuleId = module.Id,
            OrderedLessonIds = [second.Id, first.Id]
        });

        Assert.Equal(0, second.DisplayOrder);
        Assert.Equal(1, first.DisplayOrder);
    }

    [Fact]
    public async Task ReorderLessonsUseCase_WhenIdSetDoesNotMatch_ShouldThrowApplicationValidationException()
    {
        var courseModules = new FakeCourseModuleRepository();
        var lessons = new FakeLessonRepository();
        var module = CreateModule();
        module.AddLesson(Lesson.Create(module.Id, "First", "Description", 0));
        courseModules.Modules.Add(module);
        var useCase = new ReorderLessonsUseCase(courseModules, lessons, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ApplicationValidationException>(() => useCase.ExecuteAsync(new ReorderLessonsInput
        {
            ModuleId = module.Id,
            OrderedLessonIds = [Guid.NewGuid()]
        }));
    }

    private static CourseModule CreateModule()
    {
        var course = Course.Create("Course", Slug.Create($"course-{Guid.NewGuid():N}"), "Description", 0);

        return CourseModule.Create(course.Id, "Module", "Description", 0);
    }
}
