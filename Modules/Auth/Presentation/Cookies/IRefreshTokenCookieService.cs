namespace CourseCore.Api.Modules.Auth.Presentation.Cookies;

public interface IRefreshTokenCookieService
{
    void Append(HttpResponse response, string refreshToken, bool rememberMe = true);

    string? Read(HttpRequest request);

    void Delete(HttpResponse response);
}
