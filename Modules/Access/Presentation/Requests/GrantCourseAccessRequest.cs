namespace CourseCore.Api.Modules.Access.Presentation.Requests;

public class GrantCourseAccessRequest
{
    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }
}
