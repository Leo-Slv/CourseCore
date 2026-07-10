using CourseCore.Api.Modules.Progress.Application.DTOs;
using CourseCore.Api.Modules.Progress.Presentation.Requests;
using CourseCore.Api.Modules.Progress.Presentation.Responses;

namespace CourseCore.Api.Modules.Progress.Presentation.Presenters;

public static class ProgressPresenter
{
    public static RegisterLessonProgressInput ToInput(RegisterLessonProgressRequest request)
    {
        return new RegisterLessonProgressInput
        {
            UserId = request.UserId,
            LessonId = request.LessonId,
            WatchedSeconds = request.WatchedSeconds,
            MarkAsCompleted = request.MarkAsCompleted
        };
    }

    public static GetCourseProgressInput ToInput(GetCourseProgressRequest request)
    {
        return new GetCourseProgressInput
        {
            UserId = request.UserId,
            CourseId = request.CourseId
        };
    }

    public static LessonProgressResponse ToResponse(LessonProgressOutput output)
    {
        return new LessonProgressResponse
        {
            Id = output.Id,
            UserId = output.UserId,
            LessonId = output.LessonId,
            Completed = output.Completed,
            WatchedSeconds = output.WatchedSeconds,
            LastWatchedAt = output.LastWatchedAt,
            CompletedAt = output.CompletedAt
        };
    }

    public static CourseProgressResponse ToResponse(CourseProgressOutput output)
    {
        return new CourseProgressResponse
        {
            Id = output.Id,
            UserId = output.UserId,
            CourseId = output.CourseId,
            ProgressPercent = output.ProgressPercent,
            StartedAt = output.StartedAt,
            CompletedAt = output.CompletedAt,
            Lessons = output.Lessons.Select(ToResponse).ToList()
        };
    }
}
