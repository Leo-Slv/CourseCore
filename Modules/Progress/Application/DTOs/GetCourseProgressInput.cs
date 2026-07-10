namespace CourseCore.Api.Modules.Progress.Application.DTOs;

public class GetCourseProgressInput
{
    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }
}
