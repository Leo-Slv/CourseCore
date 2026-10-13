using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using CourseCore.Api.Shared.Presentation.Observability;
using CourseCore.Api.Shared.Presentation.Responses;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Shared.Presentation.RateLimiting;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddCourseCoreRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RateLimitOptions>(configuration.GetSection("RateLimiting"));

        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiterOptions.OnRejected = WriteTooManyRequestsResponseAsync;

            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.AuthLogin,
                context => CreateFixedWindowPartition(
                    context,
                    GetOptions(context).Login));
            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.AuthRefresh,
                context => CreateFixedWindowPartition(
                    context,
                    GetOptions(context).Refresh));
            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.AuthLogout,
                context => CreateFixedWindowPartition(
                    context,
                    GetOptions(context).Logout));
            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.AuthRegister,
                context => CreateFixedWindowPartition(
                    context,
                    GetOptions(context).Register));
            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.AuthResendConfirmation,
                context => CreateFixedWindowPartition(
                    context,
                    GetOptions(context).ResendConfirmation));
            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.AuthForgotPassword,
                context => CreateFixedWindowPartition(
                    context,
                    GetOptions(context).ForgotPassword));
            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.AuthResetPassword,
                context => CreateFixedWindowPartition(
                    context,
                    GetOptions(context).ResetPassword));
            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.AuthChangePassword,
                context => CreateFixedWindowPartition(
                    context,
                    GetOptions(context).ChangePassword));
            // Signed-in endpoints are limited per user, so one account cannot spend another's budget
            // and users behind the same proxy IP do not share one.
            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.Checkout,
                context => CreateFixedWindowPartition(
                    context,
                    GetOptions(context).Checkout,
                    GetUserOrIpPartitionKey(context)));
            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.Upload,
                context => CreateFixedWindowPartition(
                    context,
                    GetOptions(context).Upload,
                    GetUserOrIpPartitionKey(context)));
            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.UserContent,
                context => CreateFixedWindowPartition(
                    context,
                    GetOptions(context).UserContent,
                    GetUserOrIpPartitionKey(context)));

            // Applies to every request on top of the named policies; health probes are exempt.
            rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.Request.Path.StartsWithSegments("/health")
                    ? RateLimitPartition.GetNoLimiter("health")
                    : CreateFixedWindowPartition(
                        context,
                        GetOptions(context).Global,
                        GetUserOrIpPartitionKey(context)));

            rateLimiterOptions.AddPolicy(
                RateLimitPolicyNames.VisitorRegistration,
                context => CreateFixedWindowPartition(
                    context,
                    GetOptions(context).VisitorRegistration));
        });

        return services;
    }

    private static RateLimitOptions GetOptions(HttpContext context)
    {
        return context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(
        HttpContext context,
        RateLimitPolicyOptions options,
        string? partitionKey = null)
    {
        partitionKey ??= GetRemoteIpPartitionKey(context);
        var permitLimit = Math.Max(1, options.PermitLimit);
        var windowSeconds = Math.Max(1, options.WindowSeconds);
        var queueLimit = Math.Max(0, options.QueueLimit);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = queueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }

    /// <summary>Needs UseAuthentication to run first so the signed-in user is known.</summary>
    private static string GetUserOrIpPartitionKey(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return string.IsNullOrEmpty(userId)
            ? $"ip:{GetRemoteIpPartitionKey(context)}"
            : $"user:{userId}";
    }

    private static string GetRemoteIpPartitionKey(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static async ValueTask WriteTooManyRequestsResponseAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers["Retry-After"] = Math.Ceiling(retryAfter.TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }

        var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        var correlationId = CorrelationIdConstants.GetFromItems(context.HttpContext) ?? string.Empty;

        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ApiErrorResponse
            {
                StatusCode = StatusCodes.Status429TooManyRequests,
                Error = "Too Many Requests",
                Message = "Too many requests. Please try again later.",
                TraceId = traceId,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            },
            cancellationToken);
    }
}
