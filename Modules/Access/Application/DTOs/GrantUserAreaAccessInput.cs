namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class GrantUserAreaAccessInput
{
    public Guid UserId { get; init; }

    public Guid AreaId { get; init; }

    public bool CanView { get; init; }

    public bool CanManage { get; init; }

    public DateTime? StartsAt { get; init; }

    public DateTime? ExpiresAt { get; init; }
}
