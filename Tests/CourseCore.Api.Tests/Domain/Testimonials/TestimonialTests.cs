using CourseCore.Api.Modules.Testimonials.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Tests.Domain.Testimonials;

public class TestimonialTests
{
    [Fact]
    public void Create_WhenDataIsValid_ShouldCreateUnpublishedTestimonial()
    {
        var testimonial = Testimonial.Create("Marina Souza", "Great course!", null, null);

        Assert.False(testimonial.Published);
        Assert.Equal("Marina Souza", testimonial.AuthorName);
        Assert.Equal("Great course!", testimonial.Quote);
        Assert.Null(testimonial.CourseId);
    }

    [Fact]
    public void Create_WhenAuthorNameIsEmpty_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => Testimonial.Create(string.Empty, "Quote", null, null));
    }

    [Fact]
    public void Create_WhenQuoteIsEmpty_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => Testimonial.Create("Author", string.Empty, null, null));
    }

    [Fact]
    public void Create_WithCourseId_ShouldAttributeToCourse()
    {
        var courseId = Guid.NewGuid();

        var testimonial = Testimonial.Create("Author", "Quote", null, courseId);

        Assert.Equal(courseId, testimonial.CourseId);
    }

    [Fact]
    public void Publish_WhenCalled_ShouldMarkAsPublished()
    {
        var testimonial = Testimonial.Create("Author", "Quote", null, null);

        testimonial.Publish();

        Assert.True(testimonial.Published);
    }

    [Fact]
    public void Unpublish_WhenCalled_ShouldMarkAsUnpublished()
    {
        var testimonial = Testimonial.Create("Author", "Quote", null, null);
        testimonial.Publish();

        testimonial.Unpublish();

        Assert.False(testimonial.Published);
    }

    [Fact]
    public void ChangeContent_WhenCalled_ShouldUpdateFields()
    {
        var testimonial = Testimonial.Create("Author", "Quote", null, null);
        var newCourseId = Guid.NewGuid();

        testimonial.ChangeContent("New Author", "New Quote", "https://example.com/avatar.png", newCourseId);

        Assert.Equal("New Author", testimonial.AuthorName);
        Assert.Equal("New Quote", testimonial.Quote);
        Assert.Equal("https://example.com/avatar.png", testimonial.AvatarUrl);
        Assert.Equal(newCourseId, testimonial.CourseId);
    }
}
