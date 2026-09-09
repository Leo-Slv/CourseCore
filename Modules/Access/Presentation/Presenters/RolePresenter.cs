using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Presentation.Responses;

namespace CourseCore.Api.Modules.Access.Presentation.Presenters;

public static class RolePresenter
{
    public static RoleResponse ToResponse(RoleOutput output)
    {
        return new RoleResponse
        {
            Id = output.Id,
            Name = output.Name
        };
    }
}
