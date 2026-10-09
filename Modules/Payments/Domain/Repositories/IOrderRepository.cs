using CourseCore.Api.Modules.Payments.Domain.Entities;

namespace CourseCore.Api.Modules.Payments.Domain.Repositories;

public interface IOrderRepository
{
    Task<Order?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Order?> FindByCheckoutSessionIdAsync(string sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Order>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task CreateAsync(Order order, CancellationToken cancellationToken = default);

    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}
