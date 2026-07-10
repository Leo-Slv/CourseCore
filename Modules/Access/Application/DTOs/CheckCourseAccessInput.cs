namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class CheckCourseAccessInput
{
    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }
}
