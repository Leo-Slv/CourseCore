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

    private static Lesson CreateLesson()
    {
        return Lesson.Create(Guid.NewGuid(), "Lesson", "Description", 0);
    }
}
