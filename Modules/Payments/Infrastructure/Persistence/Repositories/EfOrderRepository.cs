using CourseCore.Api.Modules.Payments.Domain.Entities;
using CourseCore.Api.Modules.Payments.Domain.Repositories;
using CourseCore.Api.Modules.Payments.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Payments.Infrastructure.Persistence.Repositories;

public class EfOrderRepository : IOrderRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfOrderRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : OrderMapper.ToDomain(model);
    }

    public async Task<Order?> FindByCheckoutSessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StripeCheckoutSessionId == sessionId, cancellationToken);

        return model is null ? null : OrderMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<Order>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return models.Select(OrderMapper.ToDomain).ToList();
    }

    public async Task CreateAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(OrderMapper.ToPersistence(order), cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Orders.FirstAsync(x => x.Id == order.Id, cancellationToken);
        OrderMapper.ApplyChanges(order, model);
    }
}
