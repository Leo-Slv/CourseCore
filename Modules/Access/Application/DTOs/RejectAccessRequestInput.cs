namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class RejectAccessRequestInput
{
    public Guid AccessRequestId { get; init; }

    public Guid DecidedByUserId { get; init; }
}
