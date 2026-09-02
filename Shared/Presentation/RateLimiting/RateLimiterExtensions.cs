using System.Diagnostics;
using System.Globalization;
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
        });

        return services;
    }

    private static RateLimitOptions GetOptions(HttpContext context)
    {
        return context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(
        HttpContext context,
        RateLimitPolicyOptions options)
    {
        var partitionKey = GetRemoteIpPartitionKey(context);
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
