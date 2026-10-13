using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Auth.Application.Constants;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

internal static class RoleRules
{
    public static bool IsAdmin(Role role)
    {
        return string.Equals(role.Name, AuthRoleNames.Admin, StringComparison.OrdinalIgnoreCase);
    }
}
