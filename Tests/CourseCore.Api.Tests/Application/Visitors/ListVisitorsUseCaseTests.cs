using CourseCore.Api.Modules.Visitors.Application.DTOs;
using CourseCore.Api.Modules.Visitors.Application.UseCases;
using CourseCore.Api.Modules.Visitors.Domain.Entities;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Application.Validation;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Visitors;

public class ListVisitorsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnPagedVisitorsNewestFirst()
    {
        var visitors = new FakeVisitorRepository();
        var older = Restore("Older Visitor", DateTime.UtcNow.AddDays(-1));
        var newer = Restore("Newer Visitor", DateTime.UtcNow);
        visitors.Visitors.AddRange([older, newer]);
        var useCase = new ListVisitorsUseCase(visitors);

        var output = await useCase.ExecuteAsync(new ListVisitorsInput { Page = 1, PageSize = 1 });

        var item = Assert.Single(output.Items);
        Assert.Equal(newer.Id, item.Id);
        Assert.Equal(2, output.TotalItems);
        Assert.Equal(2, output.TotalPages);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrimSearchAndTreatBlankAsNull()
    {
        var visitors = new FakeVisitorRepository();
        var useCase = new ListVisitorsUseCase(visitors);

        await useCase.ExecuteAsync(new ListVisitorsInput { Search = "  ana  " });
        Assert.Equal("ana", visitors.LastSearch);

        await useCase.ExecuteAsync(new ListVisitorsInput { Search = "   " });
        Assert.Null(visitors.LastSearch);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, PaginationLimits.MaximumPageSize + 1)]
    public async Task ExecuteAsync_WhenPaginationIsInvalid_ShouldThrowValidation(int page, int pageSize)
    {
        var useCase = new ListVisitorsUseCase(new FakeVisitorRepository());

        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            useCase.ExecuteAsync(new ListVisitorsInput { Page = page, PageSize = pageSize }));
    }

    private static Visitor Restore(string name, DateTime createdAt)
    {
        return Visitor.Restore(
            Guid.NewGuid(),
            name,
            "11987654321",
            Email.Create("visitor@example.com"),
            null,
            createdAt,
            createdAt);
    }
}
