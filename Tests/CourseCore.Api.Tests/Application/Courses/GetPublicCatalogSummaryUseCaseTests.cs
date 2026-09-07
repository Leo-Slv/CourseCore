using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Courses;

public class GetPublicCatalogSummaryUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCountOnlyActiveAreas()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        areas.Areas.Add(TestEntityFactory.Area(active: true));
        areas.Areas.Add(TestEntityFactory.Area(active: true));
        areas.Areas.Add(TestEntityFactory.Area(active: false));
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas);

        var output = await useCase.ExecuteAsync();

        Assert.Equal(2, output.ActiveAreaCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCountOnlyPublishedCourses()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        var publishedCourse = TestEntityFactory.PublishedCourse(area.Id);
        var draftCourse = Course.Create(
            "Draft", Slug.Create($"draft-{Guid.NewGuid():N}"), "Draft", 0);
        courses.Courses.Add(publishedCourse);
        courses.Courses.Add(draftCourse);
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas);

        var output = await useCase.ExecuteAsync();

        Assert.Equal(1, output.PublishedCourseCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAtMostThreeFeaturedCourses()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        for (var i = 0; i < 5; i++)
        {
            courses.Courses.Add(TestEntityFactory.PublishedCourse(area.Id));
        }
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas);

        var output = await useCase.ExecuteAsync();

        Assert.Equal(3, output.FeaturedCourses.Count);
        Assert.Equal(5, output.PublishedCourseCount);
    }

    [Fact]
    public async Task ExecuteAsync_FeaturedCourseShouldNotExposeAccessOrPricingFields()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        courses.Courses.Add(TestEntityFactory.PublishedCourse(area.Id, CoursePricingModel.Paid));
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas);

        var output = await useCase.ExecuteAsync();

        var featured = Assert.Single(output.FeaturedCourses);
        Assert.NotEqual(Guid.Empty, featured.Id);
        Assert.False(string.IsNullOrEmpty(featured.Title));
    }

    [Fact]
    public async Task ExecuteAsync_WhenACourseIsFeatured_ShouldReturnHighlightedCourse()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        courses.Courses.Add(TestEntityFactory.PublishedCourse(area.Id));
        var featuredCourse = TestEntityFactory.PublishedCourse(area.Id, isFeatured: true);
        courses.Courses.Add(featuredCourse);
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas);

        var output = await useCase.ExecuteAsync();

        Assert.NotNull(output.HighlightedCourse);
        Assert.Equal(featuredCourse.Id, output.HighlightedCourse!.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoCourseIsFeatured_ShouldReturnNullHighlightedCourse()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        courses.Courses.Add(TestEntityFactory.PublishedCourse(area.Id));
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas);

        var output = await useCase.ExecuteAsync();

        Assert.Null(output.HighlightedCourse);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPerAreaPublishedCourseCounts()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var areaWithCourses = TestEntityFactory.Area();
        var areaWithoutCourses = TestEntityFactory.Area();
        areas.Areas.Add(areaWithCourses);
        areas.Areas.Add(areaWithoutCourses);
        courses.Courses.Add(TestEntityFactory.PublishedCourse(areaWithCourses.Id));
        courses.Courses.Add(TestEntityFactory.PublishedCourse(areaWithCourses.Id));
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas);

        var output = await useCase.ExecuteAsync();

        Assert.Equal(2, output.Areas.Count);
        Assert.Equal(2, output.Areas.Single(a => a.Id == areaWithCourses.Id).PublishedCourseCount);
        Assert.Equal(0, output.Areas.Single(a => a.Id == areaWithoutCourses.Id).PublishedCourseCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExcludeInactiveAreasFromAreasList()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var activeArea = TestEntityFactory.Area(active: true);
        var inactiveArea = TestEntityFactory.Area(active: false);
        areas.Areas.Add(activeArea);
        areas.Areas.Add(inactiveArea);
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas);

        var output = await useCase.ExecuteAsync();

        var singleArea = Assert.Single(output.Areas);
        Assert.Equal(activeArea.Id, singleArea.Id);
    }
}
