namespace CourseCore.Api.Modules.Access.Presentation.Responses;

public class UserCourseAccessResponse
{
    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }

    public string Source { get; init; } = string.Empty;

    public DateTime GrantedAt { get; init; }
}
