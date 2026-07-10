namespace CourseCore.Api.Modules.Progress.Presentation.Requests;

public class GetCourseProgressRequest
{
    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }
}
