using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class ListRolesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnOnlyActiveRolesOrderedByName()
    {
        var roles = new FakeRoleRepository();
        var admin = TestEntityFactory.Role(name: "Admin");
        var student = TestEntityFactory.Role(name: "Student");
        var inactive = TestEntityFactory.Role(name: "Inactive", active: false);
        await roles.CreateAsync(admin);
        await roles.CreateAsync(student);
        await roles.CreateAsync(inactive);
        var useCase = new ListRolesUseCase(roles);

        var output = await useCase.ExecuteAsync();

        Assert.Equal(2, output.Count);
        Assert.Equal("Admin", output.ElementAt(0).Name);
        Assert.Equal("Student", output.ElementAt(1).Name);
        Assert.DoesNotContain(output, r => r.Name == "Inactive");
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoRolesExist_ShouldReturnEmpty()
    {
        var useCase = new ListRolesUseCase(new FakeRoleRepository());

        var output = await useCase.ExecuteAsync();

        Assert.Empty(output);
    }
}
