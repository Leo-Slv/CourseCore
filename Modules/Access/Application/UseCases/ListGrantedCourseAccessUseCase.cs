using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.Access.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ListGrantedCourseAccessUseCase
{
    private readonly IAccessRequestRepository _accessRequests;

    public ListGrantedCourseAccessUseCase(IAccessRequestRepository accessRequests)
    {
        _accessRequests = accessRequests;
    }

    public async Task<IReadOnlyCollection<AccessRequestOutput>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var requests = await _accessRequests.ListByUserIdAsync(userId, cancellationToken);

        return requests
            .Where(request => request.Status == AccessRequestStatus.Approved)
            .Select(AccessRequestOutput.FromAccessRequest)
            .ToList();
    }
}
