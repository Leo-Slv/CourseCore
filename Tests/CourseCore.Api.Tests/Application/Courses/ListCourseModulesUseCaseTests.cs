using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Courses;

public class ListCourseModulesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCourseDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new ListCourseModulesUseCase(new FakeCourseRepository(), new FakeVideoRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnModulesRegardlessOfAdminOwnCourseAccess()
    {
        var courses = new FakeCourseRepository();
        var videos = new FakeVideoRepository();
        var course = Course.Create("Course", Slug.Create($"course-{Guid.NewGuid():N}"), "Description", 0);
        var module = CourseModule.Create(course.Id, "Module", "Description", 0);
        var lesson = Lesson.Create(module.Id, "Lesson", "Description", 0);
        module.AddLesson(lesson);
        course.AddModule(module);
        // Course is intentionally left unpublished/locked with no area access granted —
        // an admin's own CourseAccessService check would deny access, but this admin-facing
        // read path must not gate on that.
        courses.Courses.Add(course);
        var video = Video.Create(lesson.Id, "Video", "Description", VideoStorageProvider.YouTube, "dQw4w9WgXcQ", 120, 0);
        videos.Videos.Add(video);
        var useCase = new ListCourseModulesUseCase(courses, videos);

        var output = await useCase.ExecuteAsync(course.Id);

        var moduleOutput = Assert.Single(output);
        Assert.Equal(module.Id, moduleOutput.Id);
        var lessonOutput = Assert.Single(moduleOutput.Lessons);
        Assert.Equal(video.Id, lessonOutput.VideoId);
        Assert.Equal(120, lessonOutput.DurationSeconds);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldOrderModulesByDisplayOrder()
    {
        var courses = new FakeCourseRepository();
        var videos = new FakeVideoRepository();
        var course = Course.Create("Course", Slug.Create($"course-{Guid.NewGuid():N}"), "Description", 0);
        var second = CourseModule.Create(course.Id, "Second", "Description", 1);
        var first = CourseModule.Create(course.Id, "First", "Description", 0);
        course.AddModule(second);
        course.AddModule(first);
        courses.Courses.Add(course);
        var useCase = new ListCourseModulesUseCase(courses, videos);

        var output = await useCase.ExecuteAsync(course.Id);

        Assert.Equal(new[] { "First", "Second" }, output.Select(m => m.Title));
    }
}
