using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Progress.Application.DTOs;
using CourseCore.Api.Modules.Progress.Application.UseCases;
using CourseCore.Api.Modules.Progress.Domain.Entities;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Progress;

public class GetCourseProgressUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNoProgressYet_ShouldReturnZeroPercentPerModule()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(new GetCourseProgressInput
        {
            UserId = fixture.UserId,
            CourseId = fixture.CourseId
        });

        Assert.Equal(2, output.Modules.Count);
        Assert.All(output.Modules, module => Assert.Equal(0, module.ProgressPercent));
        var moduleA = output.Modules.Single(m => m.ModuleId == fixture.ModuleAId);
        Assert.Equal(2, moduleA.LessonCount);
        var moduleB = output.Modules.Single(m => m.ModuleId == fixture.ModuleBId);
        Assert.Equal(1, moduleB.LessonCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOneOfTwoLessonsInAModuleIsCompleted_ShouldReturnHalfPercentForThatModuleOnly()
    {
        var fixture = CreateFixture();
        var completedLesson = UserLessonProgress.Create(fixture.UserId, fixture.ModuleALessonIds[0]);
        completedLesson.RegisterWatch(100);
        completedLesson.RecalculateCompletion(true);
        await fixture.Progress.SaveLessonProgressAsync(completedLesson);

        var courseProgress = UserCourseProgress.Create(fixture.UserId, fixture.CourseId);
        courseProgress.Recalculate(33.33m);
        await fixture.Progress.SaveCourseProgressAsync(courseProgress);

        var output = await fixture.UseCase.ExecuteAsync(new GetCourseProgressInput
        {
            UserId = fixture.UserId,
            CourseId = fixture.CourseId
        });

        var moduleA = output.Modules.Single(m => m.ModuleId == fixture.ModuleAId);
        var moduleB = output.Modules.Single(m => m.ModuleId == fixture.ModuleBId);
        Assert.Equal(50, moduleA.ProgressPercent);
        Assert.Equal(1, moduleA.CompletedLessonCount);
        Assert.Equal(0, moduleB.ProgressPercent);
        Assert.Equal(33.33m, output.ProgressPercent);
    }

    [Fact]
    public async Task ExecuteAsync_WhenModuleHasNoLessons_ShouldReturnZeroPercentNotDivideByZero()
    {
        var fixture = CreateFixture();
        var emptyModule = CourseModule.Create(fixture.CourseId, "Empty Module", "Empty", 2);
        fixture.Course.AddModule(emptyModule);

        var output = await fixture.UseCase.ExecuteAsync(new GetCourseProgressInput
        {
            UserId = fixture.UserId,
            CourseId = fixture.CourseId
        });

        var emptyModuleOutput = output.Modules.Single(m => m.ModuleId == emptyModule.Id);
        Assert.Equal(0, emptyModuleOutput.LessonCount);
        Assert.Equal(0, emptyModuleOutput.ProgressPercent);
    }

    private static GetCourseProgressFixture CreateFixture()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var progress = new FakeProgressRepository();
        var user = TestEntityFactory.User();
        var area = TestEntityFactory.Area();
        var course = TestEntityFactory.PublishedCourse(area.Id);

        var moduleA = CourseModule.Create(course.Id, "Module A", "Module A", 0);
        var lessonA1 = Lesson.Create(moduleA.Id, "Lesson A1", "Lesson A1", 0);
        var lessonA2 = Lesson.Create(moduleA.Id, "Lesson A2", "Lesson A2", 1);
        moduleA.AddLesson(lessonA1);
        moduleA.AddLesson(lessonA2);

        var moduleB = CourseModule.Create(course.Id, "Module B", "Module B", 1);
        var lessonB1 = Lesson.Create(moduleB.Id, "Lesson B1", "Lesson B1", 0);
        moduleB.AddLesson(lessonB1);

        course.AddModule(moduleA);
        course.AddModule(moduleB);

        users.Add(user);
        areas.Areas.Add(area);
        areas.UserAreaAccesses.Add(UserAreaAccess.Create(user.Id, area.Id, canView: true, canManage: false));
        courses.Courses.Add(course);

        var courseAccessService = new CourseAccessService(users, roles, areas, courses);
        var useCase = new GetCourseProgressUseCase(users, courses, progress, courseAccessService);

        return new GetCourseProgressFixture(
            useCase,
            progress,
            course,
            user.Id,
            course.Id,
            moduleA.Id,
            moduleB.Id,
            [lessonA1.Id, lessonA2.Id]);
    }

    private sealed record GetCourseProgressFixture(
        GetCourseProgressUseCase UseCase,
        FakeProgressRepository Progress,
        Course Course,
        Guid UserId,
        Guid CourseId,
        Guid ModuleAId,
        Guid ModuleBId,
        IReadOnlyList<Guid> ModuleALessonIds);
}
