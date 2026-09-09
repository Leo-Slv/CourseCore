using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
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
        var videos = new FakeVideoRepository();
        areas.Areas.Add(TestEntityFactory.Area(active: true));
        areas.Areas.Add(TestEntityFactory.Area(active: true));
        areas.Areas.Add(TestEntityFactory.Area(active: false));
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas, videos);

        var output = await useCase.ExecuteAsync();

        Assert.Equal(2, output.ActiveAreaCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCountOnlyPublishedCourses()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var videos = new FakeVideoRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        var publishedCourse = TestEntityFactory.PublishedCourse(area.Id);
        var draftCourse = Course.Create(
            "Draft", Slug.Create($"draft-{Guid.NewGuid():N}"), "Draft", 0);
        courses.Courses.Add(publishedCourse);
        courses.Courses.Add(draftCourse);
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas, videos);

        var output = await useCase.ExecuteAsync();

        Assert.Equal(1, output.PublishedCourseCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAtMostThreeFeaturedCourses()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var videos = new FakeVideoRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        for (var i = 0; i < 5; i++)
        {
            courses.Courses.Add(TestEntityFactory.PublishedCourse(area.Id));
        }
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas, videos);

        var output = await useCase.ExecuteAsync();

        Assert.Equal(3, output.FeaturedCourses.Count);
        Assert.Equal(5, output.PublishedCourseCount);
    }

    [Fact]
    public async Task ExecuteAsync_FeaturedCourseShouldIncludePricingModuleLessonDurationAndAreaFields()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var videos = new FakeVideoRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        var course = TestEntityFactory.PublishedCourse(area.Id, CoursePricingModel.Paid);
        var module = CourseModule.Create(course.Id, "Module", "Module", 0);
        var lessonOne = Lesson.Create(module.Id, "Lesson 1", "Lesson 1", 0);
        var lessonTwo = Lesson.Create(module.Id, "Lesson 2", "Lesson 2", 1);
        module.AddLesson(lessonOne);
        module.AddLesson(lessonTwo);
        course.AddModule(module);
        courses.Courses.Add(course);
        videos.Videos.Add(Video.Create(
            lessonOne.Id, "Video 1", "Description", VideoStorageProvider.YouTube, "yt-1", 300, 0));
        videos.Videos.Add(Video.Create(
            lessonTwo.Id, "Video 2", "Description", VideoStorageProvider.YouTube, "yt-2", 180, 0));
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas, videos);

        var output = await useCase.ExecuteAsync();

        var featured = Assert.Single(output.FeaturedCourses);
        Assert.Equal("Paid", featured.PricingModel);
        Assert.Equal(course.PriceAmount, featured.PriceAmount);
        Assert.Equal(1, featured.ModuleCount);
        Assert.Equal(2, featured.LessonCount);
        Assert.Equal(480, featured.DurationSeconds);
        Assert.Equal(area.Name, featured.AreaName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenACourseIsFeatured_ShouldReturnHighlightedCourse()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var videos = new FakeVideoRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        courses.Courses.Add(TestEntityFactory.PublishedCourse(area.Id));
        var featuredCourse = TestEntityFactory.PublishedCourse(area.Id, isFeatured: true);
        courses.Courses.Add(featuredCourse);
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas, videos);

        var output = await useCase.ExecuteAsync();

        Assert.NotNull(output.HighlightedCourse);
        Assert.Equal(featuredCourse.Id, output.HighlightedCourse!.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHighlightedCourseIsAlsoFeatured_ShouldNotDuplicateContentLookup()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var videos = new FakeVideoRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        var featuredAndHighlighted = TestEntityFactory.PublishedCourse(area.Id, isFeatured: true);
        var module = CourseModule.Create(featuredAndHighlighted.Id, "Module", "Module", 0);
        var lesson = Lesson.Create(module.Id, "Lesson", "Lesson", 0);
        module.AddLesson(lesson);
        featuredAndHighlighted.AddModule(module);
        courses.Courses.Add(featuredAndHighlighted);
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas, videos);

        var output = await useCase.ExecuteAsync();

        var featured = Assert.Single(output.FeaturedCourses);
        Assert.Equal(1, featured.LessonCount);
        Assert.NotNull(output.HighlightedCourse);
        Assert.Equal(1, output.HighlightedCourse!.LessonCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoCourseIsFeatured_ShouldReturnNullHighlightedCourse()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var videos = new FakeVideoRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        courses.Courses.Add(TestEntityFactory.PublishedCourse(area.Id));
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas, videos);

        var output = await useCase.ExecuteAsync();

        Assert.Null(output.HighlightedCourse);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPerAreaPublishedCourseCounts()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var videos = new FakeVideoRepository();
        var areaWithCourses = TestEntityFactory.Area();
        var areaWithoutCourses = TestEntityFactory.Area();
        areas.Areas.Add(areaWithCourses);
        areas.Areas.Add(areaWithoutCourses);
        courses.Courses.Add(TestEntityFactory.PublishedCourse(areaWithCourses.Id));
        courses.Courses.Add(TestEntityFactory.PublishedCourse(areaWithCourses.Id));
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas, videos);

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
        var videos = new FakeVideoRepository();
        var activeArea = TestEntityFactory.Area(active: true);
        var inactiveArea = TestEntityFactory.Area(active: false);
        areas.Areas.Add(activeArea);
        areas.Areas.Add(inactiveArea);
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas, videos);

        var output = await useCase.ExecuteAsync();

        var singleArea = Assert.Single(output.Areas);
        Assert.Equal(activeArea.Id, singleArea.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCourseAreaIsInactive_ShouldReturnNullAreaName()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var videos = new FakeVideoRepository();
        var inactiveArea = TestEntityFactory.Area(active: false);
        areas.Areas.Add(inactiveArea);
        courses.Courses.Add(TestEntityFactory.PublishedCourse(inactiveArea.Id));
        var useCase = new GetPublicCatalogSummaryUseCase(courses, areas, videos);

        var output = await useCase.ExecuteAsync();

        var featured = Assert.Single(output.FeaturedCourses);
        Assert.Null(featured.AreaName);
    }
}
