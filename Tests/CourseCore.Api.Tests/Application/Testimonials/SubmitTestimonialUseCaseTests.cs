using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Testimonials.Application.DTOs;
using CourseCore.Api.Modules.Testimonials.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Testimonials;

public class SubmitTestimonialUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldDeriveAuthorNameAndAvatarFromUser()
    {
        var users = new FakeUserRepository();
        var testimonials = new FakeTestimonialRepository();
        var auditLogs = new FakeAuditLogService();
        var user = TestEntityFactory.User();
        user.ChangeName("Marina Souza");
        user.ChangeAvatarUrl("https://cdn.coursecore.local/marina.png");
        users.Add(user);
        var useCase = new SubmitTestimonialUseCase(
            users, new FakeCourseRepository(), testimonials, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(new SubmitTestimonialInput
        {
            UserId = user.Id,
            Quote = "This course changed my life!"
        });

        Assert.Equal("Marina Souza", output.AuthorName);
        Assert.Equal("https://cdn.coursecore.local/marina.png", output.AvatarUrl);
        Assert.Equal(user.Id, output.SubmittedByUserId);
        Assert.False(output.Published);
        Assert.Single(testimonials.Testimonials);
        var auditLog = Assert.Single(auditLogs.Entries, e => e.Action == AuditLogActionNames.TestimonialSubmitted);
        Assert.Equal("Marina Souza", auditLog.Metadata["displayName"]);
        Assert.Equal(user.Id.ToString(), auditLog.Metadata["submittedByUserId"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCourseId_ShouldAttributeToCourse()
    {
        var users = new FakeUserRepository();
        var courses = new FakeCourseRepository();
        var user = TestEntityFactory.User();
        users.Add(user);
        var area = TestEntityFactory.Area();
        var course = TestEntityFactory.PublishedCourse(area.Id);
        courses.Courses.Add(course);
        var useCase = new SubmitTestimonialUseCase(
            users, courses, new FakeTestimonialRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        var output = await useCase.ExecuteAsync(new SubmitTestimonialInput
        {
            UserId = user.Id,
            Quote = "Great course!",
            CourseId = course.Id
        });

        Assert.Equal(course.Id, output.CourseId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCourseIdDoesNotExist_ShouldThrowNotFoundException()
    {
        var users = new FakeUserRepository();
        var user = TestEntityFactory.User();
        users.Add(user);
        var useCase = new SubmitTestimonialUseCase(
            users, new FakeCourseRepository(), new FakeTestimonialRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new SubmitTestimonialInput
        {
            UserId = user.Id,
            Quote = "Great course!",
            CourseId = Guid.NewGuid()
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new SubmitTestimonialUseCase(
            new FakeUserRepository(), new FakeCourseRepository(), new FakeTestimonialRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new SubmitTestimonialInput
        {
            UserId = Guid.NewGuid(),
            Quote = "Great course!"
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenQuoteIsEmpty_ShouldThrow()
    {
        var useCase = new SubmitTestimonialUseCase(
            new FakeUserRepository(), new FakeCourseRepository(), new FakeTestimonialRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ApplicationValidationException>(() => useCase.ExecuteAsync(new SubmitTestimonialInput
        {
            UserId = Guid.NewGuid(),
            Quote = string.Empty
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenQuoteIsTooLong_ShouldThrow()
    {
        var useCase = new SubmitTestimonialUseCase(
            new FakeUserRepository(), new FakeCourseRepository(), new FakeTestimonialRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ApplicationValidationException>(() => useCase.ExecuteAsync(new SubmitTestimonialInput
        {
            UserId = Guid.NewGuid(),
            Quote = new string('q', 1001)
        }));
    }
}
