using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class ListAreasUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNoFilterIsProvided_ShouldReturnActiveAndInactiveAreas()
    {
        var areas = new FakeAreaRepository();
        areas.Areas.Add(TestEntityFactory.Area());
        areas.Areas.Add(TestEntityFactory.Area(active: false));
        var useCase = new ListAreasUseCase(areas);

        var output = await useCase.ExecuteAsync(new ListAreasInput());

        Assert.Equal(2, output.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFilteringByActiveTrue_ShouldReturnOnlyActiveAreas()
    {
        var areas = new FakeAreaRepository();
        areas.Areas.Add(TestEntityFactory.Area());
        areas.Areas.Add(TestEntityFactory.Area(active: false));
        var useCase = new ListAreasUseCase(areas);

        var output = await useCase.ExecuteAsync(new ListAreasInput { Active = true });

        Assert.All(output, area => Assert.True(area.Active));
    }

    [Fact]
    public async Task ExecuteAsync_WhenFilteringByActiveFalse_ShouldReturnOnlyInactiveAreas()
    {
        var areas = new FakeAreaRepository();
        areas.Areas.Add(TestEntityFactory.Area());
        areas.Areas.Add(TestEntityFactory.Area(active: false));
        var useCase = new ListAreasUseCase(areas);

        var output = await useCase.ExecuteAsync(new ListAreasInput { Active = false });

        Assert.All(output, area => Assert.False(area.Active));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoAreasExist_ShouldReturnEmptyCollection()
    {
        var useCase = new ListAreasUseCase(new FakeAreaRepository());

        var output = await useCase.ExecuteAsync(new ListAreasInput());

        Assert.Empty(output);
    }
}
