namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class GetCourseDetailsInput
{
    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }
}
