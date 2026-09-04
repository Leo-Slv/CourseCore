using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class ApproveAccessRequestUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPending_ShouldGrantUserAreaAccessAndApprove()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var accessRequests = new FakeAccessRequestRepository();
        var area = TestEntityFactory.Area();
        var course = TestEntityFactory.PublishedCourse(area.Id, CoursePricingModel.Paid);
        var request = AccessRequest.Create(Guid.NewGuid(), course.Id);

        areas.Areas.Add(area);
        courses.Courses.Add(course);
        accessRequests.Requests.Add(request);

        var useCase = new ApproveAccessRequestUseCase(
            accessRequests, courses, areas, new FakeUnitOfWork(), new FakeAuditLogService());
        var decidedByUserId = Guid.NewGuid();

        var output = await useCase.ExecuteAsync(
            new ApproveAccessRequestInput { AccessRequestId = request.Id, DecidedByUserId = decidedByUserId });

        Assert.Equal("Approved", output.Status);
        var grant = Assert.Single(areas.UserAreaAccesses);
        Assert.Equal(request.UserId, grant.UserId);
        Assert.Equal(area.Id, grant.AreaId);
        Assert.True(grant.CanView);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyDecided_ShouldThrowConflictException()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var accessRequests = new FakeAccessRequestRepository();
        var area = TestEntityFactory.Area();
        var course = TestEntityFactory.PublishedCourse(area.Id, CoursePricingModel.Paid);
        var request = AccessRequest.Create(Guid.NewGuid(), course.Id);
        request.Approve(Guid.NewGuid());

        areas.Areas.Add(area);
        courses.Courses.Add(course);
        accessRequests.Requests.Add(request);

        var useCase = new ApproveAccessRequestUseCase(
            accessRequests, courses, areas, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ConflictException>(() => useCase.ExecuteAsync(
            new ApproveAccessRequestInput { AccessRequestId = request.Id, DecidedByUserId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCourseHasNoActiveLinkedAreas_ShouldThrowConflictException()
    {
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var accessRequests = new FakeAccessRequestRepository();
        var area = TestEntityFactory.Area(active: false);
        var course = TestEntityFactory.PublishedCourse(area.Id, CoursePricingModel.Paid);
        var request = AccessRequest.Create(Guid.NewGuid(), course.Id);

        areas.Areas.Add(area);
        courses.Courses.Add(course);
        accessRequests.Requests.Add(request);

        var useCase = new ApproveAccessRequestUseCase(
            accessRequests, courses, areas, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ConflictException>(() => useCase.ExecuteAsync(
            new ApproveAccessRequestInput { AccessRequestId = request.Id, DecidedByUserId = Guid.NewGuid() }));
    }
}
