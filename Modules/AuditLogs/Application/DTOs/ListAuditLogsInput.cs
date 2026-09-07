using CourseCore.Api.Shared.Application.Validation;

namespace CourseCore.Api.Modules.AuditLogs.Application.DTOs;

public sealed class ListAuditLogsInput
{
    public int Page { get; init; } = PaginationLimits.DefaultPage;

    public int PageSize { get; init; } = PaginationLimits.DefaultPageSize;
}
