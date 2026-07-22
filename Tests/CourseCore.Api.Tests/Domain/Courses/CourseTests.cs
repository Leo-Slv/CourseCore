using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Tests.Domain.Courses;

public class CourseTests
{
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
