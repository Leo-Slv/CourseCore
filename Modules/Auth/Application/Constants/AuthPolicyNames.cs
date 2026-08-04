namespace CourseCore.Api.Modules.Auth.Application.Constants;

public static class AuthPolicyNames
{
    public const string AdminOnly = "AdminOnly";
    public const string ManageUsers = "ManageUsers";
    public const string ManageUserAreaAccess = "ManageUserAreaAccess";
    public const string ManageRoleAreaAccess = "ManageRoleAreaAccess";
    public const string CheckOwnCourseAccess = "CheckOwnCourseAccess";
    public const string CheckUserCourseAccess = "CheckUserCourseAccess";
    public const string ManageCourses = "ManageCourses";
    public const string ManageVideos = "ManageVideos";
    public const string ReadProgress = "ReadProgress";
}
