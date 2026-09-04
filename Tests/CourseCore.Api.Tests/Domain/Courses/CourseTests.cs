using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Shared.Domain.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Tests.Domain.Courses;

public class CourseTests
{
    [Fact]
    public void Create_WhenPricingModelIsNotSpecified_ShouldDefaultToPaid()
    {
        var course = CreateCourse();

        Assert.Equal(CoursePricingModel.Paid, course.PricingModel);
    }

    [Fact]
    public void Create_WhenPricingModelIsFree_ShouldStorePricingModel()
    {
        var course = Course.Create("Course", Slug.Create("free-course"), "Description", 0, pricingModel: CoursePricingModel.Free);

        Assert.Equal(CoursePricingModel.Free, course.PricingModel);
    }

    [Fact]
    public void ChangePricingModel_WhenCalled_ShouldUpdatePricingModel()
    {
        var course = CreateCourse();

        course.ChangePricingModel(CoursePricingModel.Free);

        Assert.Equal(CoursePricingModel.Free, course.PricingModel);
    }

    [Fact]
    public void Create_WhenPaidWithPriceAmount_ShouldStorePriceAmount()
    {
        var course = Course.Create(
            "Course", Slug.Create("paid-course"), "Description", 0, pricingModel: CoursePricingModel.Paid, priceAmount: 149.90m);

        Assert.Equal(149.90m, course.PriceAmount);
    }

    [Fact]
    public void Create_WhenFreeWithPriceAmount_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Course.Create(
            "Course", Slug.Create("free-course-with-price"), "Description", 0, pricingModel: CoursePricingModel.Free, priceAmount: 10m));
    }

    [Fact]
    public void ChangePriceAmount_WhenCourseIsPaid_ShouldUpdatePriceAmount()
    {
        var course = CreateCourse();

        course.ChangePriceAmount(99.90m);

        Assert.Equal(99.90m, course.PriceAmount);
    }

    [Fact]
    public void ChangePriceAmount_WhenNegative_ShouldThrowDomainException()
    {
        var course = CreateCourse();

        Assert.Throws<DomainException>(() => course.ChangePriceAmount(-1m));
    }

    [Fact]
    public void ChangePriceAmount_WhenCourseIsFree_ShouldThrowDomainException()
    {
        var course = Course.Create("Course", Slug.Create("free-course"), "Description", 0, pricingModel: CoursePricingModel.Free);

        Assert.Throws<DomainException>(() => course.ChangePriceAmount(10m));
    }

    [Fact]
    public void Publish_WhenCourseExists_ShouldPublishCourse()
    {
        var course = CreateCourse();

        course.Publish();

        Assert.True(course.Published);
        Assert.NotNull(course.PublishedAt);
    }

    [Fact]
    public void Unpublish_WhenCourseIsPublished_ShouldUnpublishCourse()
    {
        var course = CreateCourse();
        course.Publish();

        course.Unpublish();

        Assert.False(course.Published);
        Assert.Null(course.PublishedAt);
    }

    [Fact]
    public void AddModule_WhenModuleIsNew_ShouldAddModule()
    {
        var course = CreateCourse();
        var module = CourseModule.Create(course.Id, "Module", "Description", 0);

        course.AddModule(module);

        Assert.Contains(module, course.Modules);
    }

    [Fact]
    public void AttachArea_WhenAreaIsNew_ShouldAttachArea()
    {
        var course = CreateCourse();
        var areaId = Guid.NewGuid();

        course.AttachArea(areaId);

        Assert.Contains(areaId, course.AreaIds);
    }

    private static Course CreateCourse()
    {
        return Course.Create("Course", Slug.Create("course"), "Description", 0);
    }
}
