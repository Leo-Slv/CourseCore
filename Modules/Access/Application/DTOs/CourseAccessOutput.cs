namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class CourseAccessOutput
{
    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }

    public bool CanAccess { get; init; }

    public string Reason { get; init; } = string.Empty;
}
