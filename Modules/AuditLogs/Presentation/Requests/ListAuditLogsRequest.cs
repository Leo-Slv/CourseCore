using CourseCore.Api.Shared.Application.Validation;

namespace CourseCore.Api.Modules.AuditLogs.Presentation.Requests;

public sealed class ListAuditLogsRequest
{
    public int Page { get; init; } = PaginationLimits.DefaultPage;

    public int PageSize { get; init; } = PaginationLimits.DefaultPageSize;
}
