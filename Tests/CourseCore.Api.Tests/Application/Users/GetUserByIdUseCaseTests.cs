using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Users.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Users;

public class GetUserByIdUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldReturnUserWithRoleNames()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var user = TestEntityFactory.User();
        users.Add(user);
        var role = TestEntityFactory.Role(name: "Admin");
        roles.AddForUser(user.Id, role);
        var useCase = new GetUserByIdUseCase(users, roles, new FakeUserCourseAccessRepository());

        var output = await useCase.ExecuteAsync(user.Id);

        Assert.Equal(user.Id, output.Id);
        Assert.Contains("Admin", output.RoleNames);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasCourseGrant_ShouldReturnCourseTitles()
    {
        var users = new FakeUserRepository();
        var course = TestEntityFactory.PublishedCourse(Guid.NewGuid());
        var courseAccesses = new FakeUserCourseAccessRepository();
        var user = TestEntityFactory.User();
        users.Add(user);
        courseAccesses.CourseTitles[course.Id] = course.Title;
        courseAccesses.Accesses.Add(UserCourseAccess.Create(user.Id, course.Id, CourseAccessSource.Manual));
        var useCase = new GetUserByIdUseCase(users, new FakeRoleRepository(), courseAccesses);

        var output = await useCase.ExecuteAsync(user.Id);

        Assert.Contains(course.Title, output.CourseTitles);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new GetUserByIdUseCase(new FakeUserRepository(), new FakeRoleRepository(), new FakeUserCourseAccessRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }
}
