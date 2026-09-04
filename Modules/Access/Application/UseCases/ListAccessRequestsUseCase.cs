using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ListAccessRequestsUseCase
{
    private readonly IAccessRequestRepository _accessRequests;

    public ListAccessRequestsUseCase(IAccessRequestRepository accessRequests)
    {
        _accessRequests = accessRequests;
    }

    public async Task<IReadOnlyCollection<AccessRequestOutput>> ExecuteAsync(
        ListAccessRequestsInput input,
        CancellationToken cancellationToken = default)
    {
        var requests = await _accessRequests.ListAsync(input.Status, cancellationToken);

        return requests.Select(AccessRequestOutput.FromAccessRequest).ToList();
    }
}
