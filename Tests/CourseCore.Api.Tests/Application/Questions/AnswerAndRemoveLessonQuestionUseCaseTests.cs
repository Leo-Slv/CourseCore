using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Questions.Application.DTOs;
using CourseCore.Api.Modules.Questions.Application.UseCases;
using CourseCore.Api.Modules.Questions.Domain.Entities;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Questions;

public class AnswerAndRemoveLessonQuestionUseCaseTests
{
    [Fact]
    public async Task AnswerLessonQuestionUseCase_WhenQuestionExists_ShouldSetAnswerFields()
    {
        var questions = new FakeLessonQuestionRepository();
        var users = new FakeUserRepository();
        var admin = TestEntityFactory.User(email: "admin@coursecore.local");
        users.Add(admin);
        var question = LessonQuestion.Create(Guid.NewGuid(), Guid.NewGuid(), "Student", "What is this about?");
        questions.Questions.Add(question);
        var auditLogs = new FakeAuditLogService();
        var useCase = new AnswerLessonQuestionUseCase(questions, users, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(new AnswerLessonQuestionInput
        {
            QuestionId = question.Id,
            AnsweredByUserId = admin.Id,
            AnswerText = "Here is the answer."
        });

        Assert.Equal("Here is the answer.", output.AnswerText);
        Assert.Equal("Test User", output.AnsweredByName);
        Assert.Equal(admin.Id, output.AnsweredByUserId);
        Assert.NotNull(output.AnsweredAt);
        var auditLog = Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.QuestionAnswered);
        Assert.Equal(admin.Id.ToString(), auditLog.Metadata["answeredByUserId"]);
    }

    [Fact]
    public async Task AnswerLessonQuestionUseCase_WhenQuestionDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new AnswerLessonQuestionUseCase(
            new FakeLessonQuestionRepository(),
            new FakeUserRepository(),
            new FakeUnitOfWork(),
            new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new AnswerLessonQuestionInput
        {
            QuestionId = Guid.NewGuid(),
            AnsweredByUserId = Guid.NewGuid(),
            AnswerText = "Answer"
        }));
    }

    [Fact]
    public async Task AnswerLessonQuestionUseCase_WhenAnswerTextIsWhitespace_ShouldThrowApplicationValidationException()
    {
        var questions = new FakeLessonQuestionRepository();
        var users = new FakeUserRepository();
        var admin = TestEntityFactory.User();
        users.Add(admin);
        var question = LessonQuestion.Create(Guid.NewGuid(), Guid.NewGuid(), "Student", "Question?");
        questions.Questions.Add(question);
        var useCase = new AnswerLessonQuestionUseCase(questions, users, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ApplicationValidationException>(() => useCase.ExecuteAsync(new AnswerLessonQuestionInput
        {
            QuestionId = question.Id,
            AnsweredByUserId = admin.Id,
            AnswerText = "   "
        }));
    }

    [Fact]
    public async Task RemoveLessonQuestionUseCase_WhenQuestionExists_ShouldRemoveIt()
    {
        var questions = new FakeLessonQuestionRepository();
        var question = LessonQuestion.Create(Guid.NewGuid(), Guid.NewGuid(), "Student", "Question?");
        questions.Questions.Add(question);
        var auditLogs = new FakeAuditLogService();
        var useCase = new RemoveLessonQuestionUseCase(questions, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(question.Id);

        Assert.Empty(questions.Questions);
        Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.QuestionRemoved);
    }

    [Fact]
    public async Task RemoveLessonQuestionUseCase_WhenQuestionDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new RemoveLessonQuestionUseCase(
            new FakeLessonQuestionRepository(),
            new FakeUnitOfWork(),
            new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }
}
