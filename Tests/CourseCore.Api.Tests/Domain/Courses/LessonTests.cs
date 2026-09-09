using CourseCore.Api.Modules.Courses.Domain.Entities;

namespace CourseCore.Api.Tests.Domain.Courses;

public class LessonTests
{
    [Fact]
    public void MarkAsFreePreview_WhenCalled_ShouldMarkLessonAsPreview()
    {
        var lesson = CreateLesson();

        lesson.MarkAsFreePreview();

        Assert.True(lesson.FreePreview);
    }

    [Fact]
    public void Publish_WhenCalled_ShouldPublishLesson()
    {
        var lesson = CreateLesson();

        lesson.Publish();

        Assert.True(lesson.Published);
    }

    [Fact]
    public void ChangeModuleId_WhenCalled_ShouldUpdateModuleIdAndTimestamp()
    {
        var lesson = CreateLesson();
        var newModuleId = Guid.NewGuid();
        var previousUpdatedAt = lesson.UpdatedAt;

        lesson.ChangeModuleId(newModuleId);

        Assert.Equal(newModuleId, lesson.ModuleId);
        Assert.True(lesson.UpdatedAt >= previousUpdatedAt);
    }

    [Fact]
    public void ChangeModuleId_WhenEmpty_ShouldThrowDomainException()
    {
        var lesson = CreateLesson();

        Assert.Throws<CourseCore.Api.Shared.Domain.Exceptions.DomainException>(
            () => lesson.ChangeModuleId(Guid.Empty));
    }

    private static Lesson CreateLesson()
    {
        return Lesson.Create(Guid.NewGuid(), "Lesson", "Description", 0);
    }
}
