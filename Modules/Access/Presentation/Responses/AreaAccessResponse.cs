namespace CourseCore.Api.Modules.Access.Presentation.Responses;

public class AreaAccessResponse
{
    public Guid AreaId { get; init; }

    public bool CanView { get; init; }

    public bool CanManage { get; init; }
}
