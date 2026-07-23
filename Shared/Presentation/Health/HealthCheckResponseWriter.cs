using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CourseCore.Api.Shared.Presentation.Health;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            entries = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration.ToString()
                })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
