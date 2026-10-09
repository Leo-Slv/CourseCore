using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Visitors.Application.DTOs;
using CourseCore.Api.Modules.Visitors.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Visitors.Application.UseCases;

public class SetVisitorContactedUseCase
{
    private readonly IVisitorRepository _visitors;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public SetVisitorContactedUseCase(
        IVisitorRepository visitors,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _visitors = visitors;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<VisitorOutput> ExecuteAsync(
        Guid visitorId,
        bool contacted,
        Guid actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (visitorId == Guid.Empty)
        {
            throw new ArgumentException("VisitorId is required.", nameof(visitorId));
        }

        if (actingUserId == Guid.Empty)
        {
            throw new ArgumentException("ActingUserId is required.", nameof(actingUserId));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var visitor = await _visitors.FindByIdAsync(visitorId, cancellationToken)
                ?? throw new NotFoundException("Visitor not found.");

            var changed = contacted
                ? visitor.MarkContacted(actingUserId)
                : visitor.ClearContacted();

            if (changed)
            {
                await _visitors.UpdateAsync(visitor, cancellationToken);
                await _auditLogs.RecordAsync(
                    contacted ? AuditLogActionNames.VisitorMarkedContacted : AuditLogActionNames.VisitorMarkedNotContacted,
                    "Visitor",
                    visitor.Id,
                    new Dictionary<string, string?> { ["displayName"] = visitor.Name },
                    userId: actingUserId,
                    cancellationToken: cancellationToken);
            }

            return VisitorOutput.FromVisitor(visitor);
        }, cancellationToken);
    }
}
