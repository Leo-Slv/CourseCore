using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Questions.Application.DTOs;
using CourseCore.Api.Modules.Questions.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Questions;

public class AskAndListLessonQuestionsUseCaseTests
{
    [Fact]
    public async Task AskLessonQuestionUseCase_WhenUserHasAccess_ShouldCreateQuestionWithDenormalizedName()
    {
        var fixture = CreateFixture(grantAccess: true);

        var output = await fixture.AskUseCase.ExecuteAsync(new AskLessonQuestionInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            QuestionText = "How does this work?"
        });

        Assert.Equal("Test User", output.AskedByName);
        Assert.Equal("How does this work?", output.QuestionText);
        Assert.Null(output.AnswerText);
        var auditLog = Assert.Single(fixture.AuditLogs.Entries, e => e.Action == AuditLogActionNames.QuestionAsked);
        Assert.Equal(fixture.UserId.ToString(), auditLog.Metadata["askedByUserId"]);
    }

    [Fact]
    public async Task AskLessonQuestionUseCase_WhenUserHasNoAccess_ShouldThrowForbiddenException()
    {
        var fixture = CreateFixture(grantAccess: false);

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.AskUseCase.ExecuteAsync(new AskLessonQuestionInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            QuestionText = "Question"
        }));
    }

    [Fact]
    public async Task AskLessonQuestionUseCase_WhenLessonDoesNotExist_ShouldThrowNotFoundException()
    {
        var fixture = CreateFixture(grantAccess: true);

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.AskUseCase.ExecuteAsync(new AskLessonQuestionInput
        {
            UserId = fixture.UserId,
            LessonId = Guid.NewGuid(),
            QuestionText = "Question"
        }));
    }

    [Fact]
    public async Task AskLessonQuestionUseCase_WhenQuestionTextIsWhitespace_ShouldThrowApplicationValidationException()
    {
        var fixture = CreateFixture(grantAccess: true);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.AskUseCase.ExecuteAsync(new AskLessonQuestionInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            QuestionText = "   "
        }));
    }

    [Fact]
    public async Task ListLessonQuestionsUseCase_WhenUserHasAccess_ShouldReturnQuestionsOrderedByCreatedAtDescending()
    {
        var fixture = CreateFixture(grantAccess: true);
        var first = await fixture.AskUseCase.ExecuteAsync(new AskLessonQuestionInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            QuestionText = "First question"
        });
        var second = await fixture.AskUseCase.ExecuteAsync(new AskLessonQuestionInput
        {
            UserId = fixture.UserId,
            LessonId = fixture.Lesson.Id,
            QuestionText = "Second question"
        });

        var outputs = await fixture.ListUseCase.ExecuteAsync(fixture.UserId, fixture.Lesson.Id);

        Assert.Equal(2, outputs.Count);
        Assert.Equal(second.Id, outputs.First().Id);
        Assert.Equal(first.Id, outputs.Last().Id);
    }

    [Fact]
    public async Task ListLessonQuestionsUseCase_WhenUserHasNoAccess_ShouldThrowForbiddenException()
    {
        var fixture = CreateFixture(grantAccess: false);

        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.ListUseCase.ExecuteAsync(fixture.UserId, fixture.Lesson.Id));
    }

    private static LessonQuestionFixture CreateFixture(bool grantAccess)
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var lessons = new FakeLessonRepository();
        var questions = new FakeLessonQuestionRepository();
        var auditLogs = new FakeAuditLogService();
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
        var askUseCase = new AskLessonQuestionUseCase(users, lessons, courses, questions, courseAccessService, unitOfWork, auditLogs);
        var listUseCase = new ListLessonQuestionsUseCase(lessons, courses, questions, courseAccessService);

        return new LessonQuestionFixture(askUseCase, listUseCase, auditLogs, user.Id, lesson);
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

    private sealed record LessonQuestionFixture(
        AskLessonQuestionUseCase AskUseCase,
        ListLessonQuestionsUseCase ListUseCase,
        FakeAuditLogService AuditLogs,
        Guid UserId,
        Lesson Lesson);
}
