using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CourseCore.Api.Shared.Presentation.Health;

public sealed class CourseCoreDbContextHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CourseCoreDbContextHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CourseCoreDbContext>();
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("Database is reachable.")
                : HealthCheckResult.Unhealthy("Database is unreachable.");
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Database is unreachable.");
        }
    }
}
