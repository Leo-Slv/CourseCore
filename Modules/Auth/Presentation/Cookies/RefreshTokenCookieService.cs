using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using Microsoft.Extensions.Options;
using CookieOptions = Microsoft.AspNetCore.Http.CookieOptions;

namespace CourseCore.Api.Modules.Auth.Presentation.Cookies;

public sealed class RefreshTokenCookieService : IRefreshTokenCookieService
{
    private readonly RefreshTokenCookieOptions _options;
    private readonly IWebHostEnvironment _environment;

    public RefreshTokenCookieService(
        IOptions<RefreshTokenCookieOptions> options,
        IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public void Append(HttpResponse response, string refreshToken)
    {
        response.Cookies.Append(
            GetCookieName(),
            refreshToken,
            BuildCookieOptions());
    }

    public string? Read(HttpRequest request)
    {
        return request.Cookies.TryGetValue(GetCookieName(), out var refreshToken)
            ? refreshToken
            : null;
    }

    public void Delete(HttpResponse response)
    {
        response.Cookies.Delete(GetCookieName(), BuildDeleteCookieOptions());
    }

    private CookieOptions BuildCookieOptions()
    {
        var secure = IsSecure();
        var sameSite = ParseSameSiteMode(_options.SameSite);

        if (sameSite == SameSiteMode.None && !secure)
        {
            throw new InvalidOperationException("Refresh token cookie SameSite=None requires Secure=true.");
        }

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = GetCookiePath(),
            Domain = string.IsNullOrWhiteSpace(_options.Domain) ? null : _options.Domain.Trim(),
            MaxAge = TimeSpan.FromDays(Math.Max(1, _options.MaxAgeDays))
        };
    }

    private CookieOptions BuildDeleteCookieOptions()
    {
        return new CookieOptions
        {
            Secure = IsSecure(),
            SameSite = ParseSameSiteMode(_options.SameSite),
            Path = GetCookiePath(),
            Domain = string.IsNullOrWhiteSpace(_options.Domain) ? null : _options.Domain.Trim()
        };
    }

    private bool IsSecure()
    {
        return _environment.IsProduction() || _options.Secure;
    }

    private string GetCookieName()
    {
        return string.IsNullOrWhiteSpace(_options.Name)
            ? "coursecore_refresh_token"
            : _options.Name.Trim();
    }

    private string GetCookiePath()
    {
        return string.IsNullOrWhiteSpace(_options.Path)
            ? "/api/auth"
            : _options.Path.Trim();
    }

    private static SameSiteMode ParseSameSiteMode(string value)
    {
        return Enum.TryParse<SameSiteMode>(value, ignoreCase: true, out var sameSite)
            ? sameSite
            : SameSiteMode.Lax;
    }
}
