using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class ListGrantedCourseAccessUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnOnlyApprovedRequests()
    {
        var accessRequests = new FakeAccessRequestRepository();
        var userId = Guid.NewGuid();
        var approvedRequest = AccessRequest.Create(userId, Guid.NewGuid());
        approvedRequest.Approve(Guid.NewGuid());
        var pendingRequest = AccessRequest.Create(userId, Guid.NewGuid());
        var rejectedRequest = AccessRequest.Create(userId, Guid.NewGuid());
        rejectedRequest.Reject(Guid.NewGuid());
        accessRequests.Requests.Add(approvedRequest);
        accessRequests.Requests.Add(pendingRequest);
        accessRequests.Requests.Add(rejectedRequest);
        var useCase = new ListGrantedCourseAccessUseCase(accessRequests);

        var output = await useCase.ExecuteAsync(userId);

        var result = Assert.Single(output);
        Assert.Equal(approvedRequest.Id, result.Id);
    }
}
