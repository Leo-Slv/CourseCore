using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class RejectAccessRequestUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPending_ShouldMarkRejected()
    {
        var accessRequests = new FakeAccessRequestRepository();
        var request = AccessRequest.Create(Guid.NewGuid(), Guid.NewGuid());
        accessRequests.Requests.Add(request);

        var useCase = new RejectAccessRequestUseCase(accessRequests, new FakeUnitOfWork(), new FakeAuditLogService());
        var decidedByUserId = Guid.NewGuid();

        var output = await useCase.ExecuteAsync(
            new RejectAccessRequestInput { AccessRequestId = request.Id, DecidedByUserId = decidedByUserId });

        Assert.Equal("Rejected", output.Status);
        Assert.Equal(decidedByUserId, output.DecidedByUserId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyDecided_ShouldThrowConflictException()
    {
        var accessRequests = new FakeAccessRequestRepository();
        var request = AccessRequest.Create(Guid.NewGuid(), Guid.NewGuid());
        request.Reject(Guid.NewGuid());
        accessRequests.Requests.Add(request);

        var useCase = new RejectAccessRequestUseCase(accessRequests, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ConflictException>(() => useCase.ExecuteAsync(
            new RejectAccessRequestInput { AccessRequestId = request.Id, DecidedByUserId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestDoesNotExist_ShouldThrowNotFoundException()
    {
        var accessRequests = new FakeAccessRequestRepository();
        var useCase = new RejectAccessRequestUseCase(accessRequests, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(
            new RejectAccessRequestInput { AccessRequestId = Guid.NewGuid(), DecidedByUserId = Guid.NewGuid() }));
    }
}
