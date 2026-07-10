namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class AreaAccessOutput
{
    public Guid AreaId { get; init; }

    public bool CanView { get; init; }

    public bool CanManage { get; init; }
}
