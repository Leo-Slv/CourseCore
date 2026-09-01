using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class GetAreaByIdUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenAreaExistsAndIsActive_ShouldReturnArea()
    {
        var areas = new FakeAreaRepository();
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        var useCase = new GetAreaByIdUseCase(areas);

        var output = await useCase.ExecuteAsync(area.Id);

        Assert.Equal(area.Id, output.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAreaExistsAndIsInactive_ShouldReturnArea()
    {
        var areas = new FakeAreaRepository();
        var area = TestEntityFactory.Area(active: false);
        areas.Areas.Add(area);
        var useCase = new GetAreaByIdUseCase(areas);

        var output = await useCase.ExecuteAsync(area.Id);

        Assert.False(output.Active);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAreaDoesNotExist_ShouldThrowNotFound()
    {
        var useCase = new GetAreaByIdUseCase(new FakeAreaRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }
}
