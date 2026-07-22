namespace CourseCore.Api.Modules.Auth.Application.Constants;

public static class AuthClaimTypes
{
    public const string UserId = System.Security.Claims.ClaimTypes.NameIdentifier;
    public const string Email = System.Security.Claims.ClaimTypes.Email;
    public const string Name = System.Security.Claims.ClaimTypes.Name;
    public const string Role = System.Security.Claims.ClaimTypes.Role;
}
