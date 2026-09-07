using CourseCore.Api.Modules.Testimonials.Application.DTOs;
using CourseCore.Api.Modules.Testimonials.Application.UseCases;
using CourseCore.Api.Modules.Testimonials.Domain.Entities;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Testimonials;

public class TestimonialUseCaseTests
{
    [Fact]
    public async Task CreateTestimonialUseCase_WhenDataIsValid_ShouldCreateUnpublishedTestimonial()
    {
        var testimonials = new FakeTestimonialRepository();
        var useCase = new CreateTestimonialUseCase(
            testimonials, new FakeCourseRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        var output = await useCase.ExecuteAsync(new CreateTestimonialInput
        {
            AuthorName = "Marina Souza",
            Quote = "Great course!"
        });

        Assert.False(output.Published);
        Assert.Single(testimonials.Testimonials);
    }

    [Fact]
    public async Task CreateTestimonialUseCase_WhenCourseIdDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new CreateTestimonialUseCase(
            new FakeTestimonialRepository(), new FakeCourseRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new CreateTestimonialInput
        {
            AuthorName = "Author",
            Quote = "Quote",
            CourseId = Guid.NewGuid()
        }));
    }

    [Fact]
    public async Task CreateTestimonialUseCase_WhenAuthorNameIsEmpty_ShouldThrowApplicationValidationException()
    {
        var useCase = new CreateTestimonialUseCase(
            new FakeTestimonialRepository(), new FakeCourseRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ApplicationValidationException>(() => useCase.ExecuteAsync(new CreateTestimonialInput
        {
            AuthorName = string.Empty,
            Quote = "Quote"
        }));
    }

    [Fact]
    public async Task UpdateTestimonialUseCase_WhenTestimonialExists_ShouldUpdateFields()
    {
        var testimonials = new FakeTestimonialRepository();
        var testimonial = Testimonial.Create("Author", "Quote", null, null);
        testimonials.Testimonials.Add(testimonial);
        var useCase = new UpdateTestimonialUseCase(
            testimonials, new FakeCourseRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        var output = await useCase.ExecuteAsync(new UpdateTestimonialInput
        {
            TestimonialId = testimonial.Id,
            AuthorName = "Updated Author",
            Quote = "Updated Quote"
        });

        Assert.Equal("Updated Author", output.AuthorName);
    }

    [Fact]
    public async Task UpdateTestimonialUseCase_WhenTestimonialDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new UpdateTestimonialUseCase(
            new FakeTestimonialRepository(), new FakeCourseRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new UpdateTestimonialInput
        {
            TestimonialId = Guid.NewGuid(),
            AuthorName = "Author",
            Quote = "Quote"
        }));
    }

    [Fact]
    public async Task PublishTestimonialUseCase_WhenTestimonialExists_ShouldPublish()
    {
        var testimonials = new FakeTestimonialRepository();
        var testimonial = Testimonial.Create("Author", "Quote", null, null);
        testimonials.Testimonials.Add(testimonial);
        var useCase = new PublishTestimonialUseCase(testimonials, new FakeUnitOfWork(), new FakeAuditLogService());

        var output = await useCase.ExecuteAsync(testimonial.Id);

        Assert.True(output.Published);
    }

    [Fact]
    public async Task UnpublishTestimonialUseCase_WhenTestimonialExists_ShouldUnpublish()
    {
        var testimonials = new FakeTestimonialRepository();
        var testimonial = Testimonial.Create("Author", "Quote", null, null);
        testimonial.Publish();
        testimonials.Testimonials.Add(testimonial);
        var useCase = new UnpublishTestimonialUseCase(testimonials, new FakeUnitOfWork(), new FakeAuditLogService());

        var output = await useCase.ExecuteAsync(testimonial.Id);

        Assert.False(output.Published);
    }

    [Fact]
    public async Task ListTestimonialsUseCase_ShouldReturnAllTestimonials()
    {
        var testimonials = new FakeTestimonialRepository();
        var published = Testimonial.Create("Author 1", "Quote 1", null, null);
        published.Publish();
        var unpublished = Testimonial.Create("Author 2", "Quote 2", null, null);
        testimonials.Testimonials.Add(published);
        testimonials.Testimonials.Add(unpublished);
        var useCase = new ListTestimonialsUseCase(testimonials);

        var output = await useCase.ExecuteAsync();

        Assert.Equal(2, output.Count);
    }

    [Fact]
    public async Task ListPublicTestimonialsUseCase_ShouldReturnOnlyPublishedTestimonials()
    {
        var testimonials = new FakeTestimonialRepository();
        var published = Testimonial.Create("Author 1", "Quote 1", null, null);
        published.Publish();
        var unpublished = Testimonial.Create("Author 2", "Quote 2", null, null);
        testimonials.Testimonials.Add(published);
        testimonials.Testimonials.Add(unpublished);
        var useCase = new ListPublicTestimonialsUseCase(testimonials);

        var output = await useCase.ExecuteAsync();

        var result = Assert.Single(output);
        Assert.Equal(published.Id, result.Id);
    }
}
