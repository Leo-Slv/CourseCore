namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class RequestCourseAccessInput
{
    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }
}
