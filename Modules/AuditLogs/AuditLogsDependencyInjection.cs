using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.AuditLogs.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Infrastructure.Persistence.Repositories;

namespace CourseCore.Api.Modules.AuditLogs;

public static class AuditLogsDependencyInjection
{
    public static IServiceCollection AddAuditLogsModule(this IServiceCollection services)
    {
        services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        return services;
    }
}
