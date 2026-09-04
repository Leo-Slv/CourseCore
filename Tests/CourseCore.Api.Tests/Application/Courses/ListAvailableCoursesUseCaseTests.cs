using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Courses;

public class ListAvailableCoursesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldAggregateModuleAndLessonCountsAndSumDurationAcrossModules()
    {
        var fixture = CreateFixture();
        var module1 = CourseModule.Create(fixture.CourseId, "Module 1", "Module 1", 0);
        var lesson1 = Lesson.Create(module1.Id, "Lesson 1", "Lesson 1", 0);
        var lesson2 = Lesson.Create(module1.Id, "Lesson 2", "Lesson 2", 1);
        module1.AddLesson(lesson1);
        module1.AddLesson(lesson2);
        var module2 = CourseModule.Create(fixture.CourseId, "Module 2", "Module 2", 1);
        var lesson3 = Lesson.Create(module2.Id, "Lesson 3", "Lesson 3", 0);
        module2.AddLesson(lesson3);
        fixture.Course.AddModule(module1);
        fixture.Course.AddModule(module2);

        fixture.Videos.Videos.Add(Video.Create(
            lesson1.Id, "V1", "V1", VideoStorageProvider.Local, "videos/v1.mp4", durationSeconds: 300, sizeBytes: 10));
        fixture.Videos.Videos.Add(Video.Create(
            lesson2.Id, "V2", "V2", VideoStorageProvider.Local, "videos/v2.mp4", durationSeconds: 180, sizeBytes: 10));
        // lesson3 intentionally has no video

        var output = await fixture.UseCase.ExecuteAsync(new ListAvailableCoursesInput { UserId = fixture.UserId });

        var item = output.Courses.Single(course => course.Id == fixture.CourseId);
        Assert.Equal(2, item.ModuleCount);
        Assert.Equal(3, item.LessonCount);
        Assert.Equal(480, item.DurationSeconds);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCourseHasNoModules_ShouldReturnZeroCountsAndZeroDuration()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(new ListAvailableCoursesInput { UserId = fixture.UserId });

        var item = output.Courses.Single(course => course.Id == fixture.CourseId);
        Assert.Equal(0, item.ModuleCount);
        Assert.Equal(0, item.LessonCount);
        Assert.Equal(0, item.DurationSeconds);
    }

    private static ListAvailableCoursesFixture CreateFixture()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var videos = new FakeVideoRepository();
        var user = TestEntityFactory.User(email: $"user-{Guid.NewGuid():N}@coursecore.local");
        var area = TestEntityFactory.Area();
        var course = TestEntityFactory.PublishedCourse(area.Id, CoursePricingModel.Free);

        users.Add(user);
        areas.Areas.Add(area);
        courses.Courses.Add(course);

        var courseAccessService = new CourseAccessService(users, roles, areas, courses);
        var useCase = new ListAvailableCoursesUseCase(courseAccessService, courses, videos);

        return new ListAvailableCoursesFixture(useCase, user.Id, course.Id, course, videos);
    }

    private sealed record ListAvailableCoursesFixture(
        ListAvailableCoursesUseCase UseCase,
        Guid UserId,
        Guid CourseId,
        Course Course,
        FakeVideoRepository Videos);
}
