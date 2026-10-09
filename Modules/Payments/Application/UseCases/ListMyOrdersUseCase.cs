using CourseCore.Api.Modules.Payments.Application.DTOs;
using CourseCore.Api.Modules.Payments.Domain.Repositories;

namespace CourseCore.Api.Modules.Payments.Application.UseCases;

public class ListMyOrdersUseCase
{
    private readonly IOrderRepository _orders;

    public ListMyOrdersUseCase(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<IReadOnlyCollection<OrderOutput>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var orders = await _orders.ListByUserIdAsync(userId, cancellationToken);

        return orders.Select(OrderOutput.FromOrder).ToList();
    }
}
