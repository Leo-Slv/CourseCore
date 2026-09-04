using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class RejectAccessRequestUseCase
{
    private readonly IAccessRequestRepository _accessRequests;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public RejectAccessRequestUseCase(
        IAccessRequestRepository accessRequests,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _accessRequests = accessRequests;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<AccessRequestOutput> ExecuteAsync(
        RejectAccessRequestInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.AccessRequestId == Guid.Empty)
        {
            throw new ArgumentException("AccessRequestId is required.", nameof(input));
        }

        if (input.DecidedByUserId == Guid.Empty)
        {
            throw new ArgumentException("DecidedByUserId is required.", nameof(input));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var request = await _accessRequests.FindByIdAsync(input.AccessRequestId, cancellationToken);

            if (request is null)
            {
                throw new NotFoundException("Access request not found.");
            }

            if (request.Status != AccessRequestStatus.Pending)
            {
                throw new ConflictException("Access request has already been decided.");
            }

            request.Reject(input.DecidedByUserId);
            await _accessRequests.UpdateAsync(request, cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.AccessRequestRejected,
                "AccessRequest",
                request.Id,
                new Dictionary<string, string?>
                {
                    ["targetUserId"] = request.UserId.ToString(),
                    ["courseId"] = request.CourseId.ToString()
                },
                userId: input.DecidedByUserId,
                cancellationToken: cancellationToken);

            return AccessRequestOutput.FromAccessRequest(request);
        }, cancellationToken);
    }
}
