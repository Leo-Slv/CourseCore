using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ListMyAccessRequestsUseCase
{
    private readonly IAccessRequestRepository _accessRequests;

    public ListMyAccessRequestsUseCase(IAccessRequestRepository accessRequests)
    {
        _accessRequests = accessRequests;
    }

    public async Task<IReadOnlyCollection<AccessRequestOutput>> ExecuteAsync(
        ListMyAccessRequestsInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(input));
        }

        var requests = await _accessRequests.ListByUserIdAsync(input.UserId, cancellationToken);

        return requests.Select(AccessRequestOutput.FromAccessRequest).ToList();
    }
}
