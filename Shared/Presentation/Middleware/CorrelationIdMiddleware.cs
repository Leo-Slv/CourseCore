using CourseCore.Api.Shared.Presentation.Observability;
using Microsoft.Extensions.Primitives;

namespace CourseCore.Api.Shared.Presentation.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context.Request.Headers);
        context.Items[CorrelationIdConstants.ItemName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdConstants.ItemName] = correlationId
        }))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(IHeaderDictionary headers)
    {
        if (headers.TryGetValue(CorrelationIdConstants.HeaderName, out var values) &&
            TryGetValidCorrelationId(values, out var correlationId))
        {
            return correlationId;
        }

        return Guid.NewGuid().ToString("D");
    }

    private static bool TryGetValidCorrelationId(
        StringValues values,
        out string correlationId)
    {
        correlationId = string.Empty;

        var candidate = values.FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(candidate) ||
            candidate.Length > CorrelationIdConstants.MaxLength ||
            !Guid.TryParse(candidate, out _))
        {
            return false;
        }

        correlationId = candidate;
        return true;
    }
}
