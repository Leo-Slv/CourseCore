using CourseCore.Api.Modules.AuditLogs.Application.DTOs;
using CourseCore.Api.Modules.AuditLogs.Domain.Repositories;
using CourseCore.Api.Shared.Application.DTOs;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Application.Validation;

namespace CourseCore.Api.Modules.AuditLogs.Application.UseCases;

public class ListAuditLogsUseCase
{
    private readonly IAuditLogRepository _auditLogs;

    public ListAuditLogsUseCase(IAuditLogRepository auditLogs)
    {
        _auditLogs = auditLogs;
    }

    public async Task<PagedResult<AuditLogOutput>> ExecuteAsync(
        ListAuditLogsInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.Page < 1)
        {
            throw new ApplicationValidationException("Page must be greater than or equal to 1.");
        }

        if (input.PageSize < 1 || input.PageSize > PaginationLimits.MaximumPageSize)
        {
            throw new ApplicationValidationException(
                $"PageSize must be between 1 and {PaginationLimits.MaximumPageSize}.");
        }

        var (items, totalCount) = await _auditLogs.ListPagedAsync(input.Page, input.PageSize, cancellationToken);

        return new PagedResult<AuditLogOutput>
        {
            Items = items.Select(AuditLogOutput.FromAuditLog).ToList(),
            Page = input.Page,
            PageSize = input.PageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)input.PageSize)
        };
    }
}
