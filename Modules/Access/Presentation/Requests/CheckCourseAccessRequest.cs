namespace CourseCore.Api.Modules.Access.Presentation.Requests;

public class CheckCourseAccessRequest
{
    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }
}
