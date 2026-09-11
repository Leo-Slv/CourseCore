using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Progress.Application.DTOs;
using CourseCore.Api.Modules.Progress.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Progress;

public class LessonNoteUseCaseTests
{
    [Fact]
    public async Task UpsertLessonNoteUseCase_WhenUserHasAccessAndNoNoteExists_ShouldCreateNote()
    {
        var fixture = CreateFixture(grantAccess: true);

        var output = await fixture.UpsertUseCase.ExecuteAsync(new UpsertLessonNoteInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            Content = "My first note"
        });

        Assert.Equal("My first note", output.Content);
        Assert.Equal(fixture.UserId, output.UserId);
        Assert.Equal(fixture.Lesson.Id, output.LessonId);
    }

    [Fact]
    public async Task UpsertLessonNoteUseCase_WhenNoteAlreadyExists_ShouldUpdateInPlace()
    {
        var fixture = CreateFixture(grantAccess: true);
        var created = await fixture.UpsertUseCase.ExecuteAsync(new UpsertLessonNoteInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            Content = "Old content"
        });

        var updated = await fixture.UpsertUseCase.ExecuteAsync(new UpsertLessonNoteInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            Content = "New content"
        });

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("New content", updated.Content);
    }

    [Fact]
    public async Task UpsertLessonNoteUseCase_WhenUserHasNoAccess_ShouldThrowForbiddenException()
    {
        var fixture = CreateFixture(grantAccess: false);

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.UpsertUseCase.ExecuteAsync(new UpsertLessonNoteInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            Content = "Note"
        }));
    }

    [Fact]
    public async Task UpsertLessonNoteUseCase_WhenLessonDoesNotExist_ShouldThrowNotFoundException()
    {
        var fixture = CreateFixture(grantAccess: true);

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UpsertUseCase.ExecuteAsync(new UpsertLessonNoteInput
        {
            UserId = fixture.UserId,
            LessonId = Guid.NewGuid(),
            Content = "Note"
        }));
    }

    [Fact]
    public async Task UpsertLessonNoteUseCase_WhenContentIsWhitespace_ShouldThrowApplicationValidationException()
    {
        var fixture = CreateFixture(grantAccess: true);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UpsertUseCase.ExecuteAsync(new UpsertLessonNoteInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            Content = "   "
        }));
    }

    [Fact]
    public async Task UpsertLessonNoteUseCase_WhenContentExceedsMaxLength_ShouldThrowApplicationValidationException()
    {
        var fixture = CreateFixture(grantAccess: true);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UpsertUseCase.ExecuteAsync(new UpsertLessonNoteInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            Content = new string('a', 10_001)
        }));
    }

    [Fact]
    public async Task GetLessonNoteUseCase_WhenNoteExists_ShouldReturnNote()
    {
        var fixture = CreateFixture(grantAccess: true);
        await fixture.UpsertUseCase.ExecuteAsync(new UpsertLessonNoteInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            Content = "Note content"
        });

        var output = await fixture.GetUseCase.ExecuteAsync(fixture.UserId, fixture.Lesson.Id);

        Assert.Equal("Note content", output.Content);
    }

    [Fact]
    public async Task GetLessonNoteUseCase_WhenNoteDoesNotExist_ShouldThrowNotFoundException()
    {
        var fixture = CreateFixture(grantAccess: true);

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.GetUseCase.ExecuteAsync(fixture.UserId, fixture.Lesson.Id));
    }

    [Fact]
    public async Task RemoveLessonNoteUseCase_WhenNoteExists_ShouldRemoveIt()
    {
        var fixture = CreateFixture(grantAccess: true);
        await fixture.UpsertUseCase.ExecuteAsync(new UpsertLessonNoteInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            Content = "Note content"
        });

        await fixture.RemoveUseCase.ExecuteAsync(fixture.UserId, fixture.Lesson.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.GetUseCase.ExecuteAsync(fixture.UserId, fixture.Lesson.Id));
    }

    [Fact]
    public async Task RemoveLessonNoteUseCase_WhenNoteDoesNotExist_ShouldThrowNotFoundException()
    {
        var fixture = CreateFixture(grantAccess: true);

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.RemoveUseCase.ExecuteAsync(fixture.UserId, fixture.Lesson.Id));
    }

    private static LessonNoteFixture CreateFixture(bool grantAccess)
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var lessons = new FakeLessonRepository();
        var notes = new FakeLessonNoteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var user = TestEntityFactory.User();
        var area = TestEntityFactory.Area();
        var (course, lesson) = CreatePublishedCourseWithLesson(area.Id);

        users.Add(user);
        areas.Areas.Add(area);
        foreach (var moduleLesson in course.Modules.SelectMany(module => module.Lessons))
        {
            lessons.Lessons.Add(moduleLesson);
        }

        courses.Courses.Add(course);

        if (grantAccess)
        {
            areas.UserAreaAccesses.Add(UserAreaAccess.Create(user.Id, area.Id, canView: true, canManage: false));
        }

        var courseAccessService = new CourseAccessService(users, roles, areas, courses);
        var upsertUseCase = new UpsertLessonNoteUseCase(users, lessons, courses, notes, courseAccessService, unitOfWork);
        var getUseCase = new GetLessonNoteUseCase(notes);
        var removeUseCase = new RemoveLessonNoteUseCase(notes, unitOfWork);

        return new LessonNoteFixture(upsertUseCase, getUseCase, removeUseCase, user.Id, lesson);
    }

    private static (Course Course, Lesson Lesson) CreatePublishedCourseWithLesson(Guid areaId)
    {
        var course = Course.Create(
            "Course",
            Slug.Create($"course-{Guid.NewGuid():N}"),
            "Description",
            displayOrder: 0);
        var module = CourseModule.Create(course.Id, "Module", "Description", displayOrder: 0);
        var lesson = Lesson.Create(module.Id, "Lesson", "Description", displayOrder: 0);
        module.AddLesson(lesson);
        course.AddModule(module);
        course.AttachArea(areaId);
        course.Publish();

        return (course, lesson);
    }

    private sealed record LessonNoteFixture(
        UpsertLessonNoteUseCase UpsertUseCase,
        GetLessonNoteUseCase GetUseCase,
        RemoveLessonNoteUseCase RemoveUseCase,
        Guid UserId,
        Lesson Lesson);
}
