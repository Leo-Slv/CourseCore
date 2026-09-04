using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Tests.Domain.Access;

public class AccessRequestTests
{
    [Fact]
    public void Create_WhenCalled_ShouldDefaultToPending()
    {
        var request = AccessRequest.Create(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(AccessRequestStatus.Pending, request.Status);
        Assert.Null(request.DecidedAt);
        Assert.Null(request.DecidedByUserId);
    }

    [Fact]
    public void Approve_WhenPending_ShouldTransitionToApproved()
    {
        var request = AccessRequest.Create(Guid.NewGuid(), Guid.NewGuid());
        var decidedByUserId = Guid.NewGuid();

        request.Approve(decidedByUserId);

        Assert.Equal(AccessRequestStatus.Approved, request.Status);
        Assert.Equal(decidedByUserId, request.DecidedByUserId);
        Assert.NotNull(request.DecidedAt);
    }

    [Fact]
    public void Reject_WhenPending_ShouldTransitionToRejected()
    {
        var request = AccessRequest.Create(Guid.NewGuid(), Guid.NewGuid());
        var decidedByUserId = Guid.NewGuid();

        request.Reject(decidedByUserId);

        Assert.Equal(AccessRequestStatus.Rejected, request.Status);
        Assert.Equal(decidedByUserId, request.DecidedByUserId);
        Assert.NotNull(request.DecidedAt);
    }

    [Fact]
    public void Approve_WhenAlreadyDecided_ShouldThrowDomainException()
    {
        var request = AccessRequest.Create(Guid.NewGuid(), Guid.NewGuid());
        request.Approve(Guid.NewGuid());

        Assert.Throws<DomainException>(() => request.Approve(Guid.NewGuid()));
    }

    [Fact]
    public void Reject_WhenAlreadyDecided_ShouldThrowDomainException()
    {
        var request = AccessRequest.Create(Guid.NewGuid(), Guid.NewGuid());
        request.Reject(Guid.NewGuid());

        Assert.Throws<DomainException>(() => request.Reject(Guid.NewGuid()));
    }
}
