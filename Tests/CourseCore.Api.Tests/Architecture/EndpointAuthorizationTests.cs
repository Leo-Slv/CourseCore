using System.Reflection;
using CourseCore.Api.Modules.Auth.Presentation.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace CourseCore.Api.Tests.Architecture;

/// <summary>
/// Default-deny guard: every HTTP action must say, explicitly, whether it is public
/// ([AllowAnonymous]) or protected ([Authorize], on the action or its controller).
/// A forgotten attribute fails here instead of becoming an open route.
/// </summary>
public class EndpointAuthorizationTests
{
    // Public by design: sign-in and registration flows, the Stripe webhook (verified by
    // signature), the landing page data and the first-time visitor form (captcha + rate limit).
    private static readonly HashSet<string> PublicEndpoints =
    [
        "POST api/auth/login",
        "POST api/auth/register",
        "POST api/auth/forgot-password",
        "POST api/auth/reset-password",
        "POST api/auth/refresh-token",
        "POST api/auth/logout",
        "POST api/payments/stripe/webhook",
        "GET api/courses/public-summary",
        "GET api/testimonials/public",
        "POST api/visitors"
    ];

    [Fact]
    public void EveryAction_ShouldDeclareAuthorizationExplicitly()
    {
        var missing = Actions()
            .Where(action => !HasAuthorize(action) && !HasAllowAnonymous(action))
            .Select(Describe)
            .ToList();

        Assert.True(
            missing.Count == 0,
            "Actions with no [Authorize] or [AllowAnonymous]: " + string.Join("; ", missing));
    }

    [Fact]
    public void OnlyTheKnownPublicEndpoints_ShouldBeAnonymous()
    {
        var anonymous = Actions()
            .Where(HasAllowAnonymous)
            .Select(Describe)
            .ToHashSet();

        Assert.Equal(PublicEndpoints.OrderBy(x => x), anonymous.OrderBy(x => x));
    }

    private static IEnumerable<MethodInfo> Actions()
    {
        return typeof(AuthController).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any());
    }

    private static bool HasAuthorize(MethodInfo action)
    {
        return action.GetCustomAttributes<AuthorizeAttribute>().Any()
            || action.DeclaringType!.GetCustomAttributes<AuthorizeAttribute>().Any();
    }

    private static bool HasAllowAnonymous(MethodInfo action)
    {
        return action.GetCustomAttributes<AllowAnonymousAttribute>().Any()
            || action.DeclaringType!.GetCustomAttributes<AllowAnonymousAttribute>().Any();
    }

    private static string Describe(MethodInfo action)
    {
        var http = action.GetCustomAttributes<HttpMethodAttribute>().First();
        var classRoute = action.DeclaringType!.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
        // A template starting with "/" is absolute: it ignores the controller route.
        var route = string.IsNullOrEmpty(http.Template)
            ? classRoute
            : http.Template.StartsWith('/') ? http.Template : $"{classRoute}/{http.Template}";

        return $"{http.HttpMethods.First()} {route.TrimStart('/')}";
    }
}
