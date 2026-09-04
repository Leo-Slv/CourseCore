using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class RequestCourseAccessUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCourseIsFree_ShouldThrowConflictException()
    {
        var fixture = CreateFixture(CoursePricingModel.Free);

        await Assert.ThrowsAsync<ConflictException>(() => fixture.UseCase.ExecuteAsync(
            new RequestCourseAccessInput { UserId = fixture.UserId, CourseId = fixture.CourseId }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserAlreadyHasAccess_ShouldThrowConflictException()
    {
        var fixture = CreateFixture(CoursePricingModel.Paid);
        fixture.Areas.UserAreaAccesses.Add(UserAreaAccess.Create(fixture.UserId, fixture.AreaId, canView: true, canManage: false));

        await Assert.ThrowsAsync<ConflictException>(() => fixture.UseCase.ExecuteAsync(
            new RequestCourseAccessInput { UserId = fixture.UserId, CourseId = fixture.CourseId }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPendingRequestAlreadyExists_ShouldThrowConflictException()
    {
        var fixture = CreateFixture(CoursePricingModel.Paid);
        fixture.AccessRequests.Requests.Add(AccessRequest.Create(fixture.UserId, fixture.CourseId));

        await Assert.ThrowsAsync<ConflictException>(() => fixture.UseCase.ExecuteAsync(
            new RequestCourseAccessInput { UserId = fixture.UserId, CourseId = fixture.CourseId }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLockedPaidCourse_ShouldCreatePendingRequest()
    {
        var fixture = CreateFixture(CoursePricingModel.Paid);

        var output = await fixture.UseCase.ExecuteAsync(
            new RequestCourseAccessInput { UserId = fixture.UserId, CourseId = fixture.CourseId });

        Assert.Equal("Pending", output.Status);
        Assert.Single(fixture.AccessRequests.Requests);
    }

    private static RequestCourseAccessFixture CreateFixture(CoursePricingModel pricingModel)
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var accessRequests = new FakeAccessRequestRepository();
        var user = TestEntityFactory.User(email: $"user-{Guid.NewGuid():N}@coursecore.local");
        var area = TestEntityFactory.Area();
        var course = TestEntityFactory.PublishedCourse(area.Id, pricingModel);

        users.Add(user);
        areas.Areas.Add(area);
        courses.Courses.Add(course);

        var courseAccessService = new CourseAccessService(users, roles, areas, courses);
        var useCase = new RequestCourseAccessUseCase(
            users, courses, accessRequests, courseAccessService, new FakeUnitOfWork(), new FakeAuditLogService());

        return new RequestCourseAccessFixture(useCase, user.Id, course.Id, area.Id, areas, accessRequests);
    }

    private sealed record RequestCourseAccessFixture(
        RequestCourseAccessUseCase UseCase,
        Guid UserId,
        Guid CourseId,
        Guid AreaId,
        FakeAreaRepository Areas,
        FakeAccessRequestRepository AccessRequests);
}
