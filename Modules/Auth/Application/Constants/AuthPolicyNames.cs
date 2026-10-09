namespace CourseCore.Api.Modules.Auth.Application.Constants;

public static class AuthPolicyNames
{
    public const string AdminOnly = "AdminOnly";
    public const string ManageUsers = "ManageUsers";
    public const string ManageCourseAccess = "ManageCourseAccess";
    public const string ManageAreas = "ManageAreas";
    public const string CheckOwnCourseAccess = "CheckOwnCourseAccess";
    public const string CheckUserCourseAccess = "CheckUserCourseAccess";
    public const string ManageCourses = "ManageCourses";
    public const string ManageVideos = "ManageVideos";
    public const string ReadProgress = "ReadProgress";
    public const string ReadAudit = "ReadAudit";
    public const string ReadVisitors = "ReadVisitors";
}
