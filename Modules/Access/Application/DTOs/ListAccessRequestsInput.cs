using CourseCore.Api.Modules.Access.Domain.Enums;

namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class ListAccessRequestsInput
{
    public AccessRequestStatus? Status { get; init; }
}
