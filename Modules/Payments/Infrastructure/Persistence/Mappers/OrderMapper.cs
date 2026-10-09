using CourseCore.Api.Modules.Payments.Domain.Entities;
using CourseCore.Api.Modules.Payments.Domain.Enums;
using CourseCore.Api.Modules.Payments.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Payments.Infrastructure.Persistence.Mappers;

public static class OrderMapper
{
    public static Order ToDomain(OrderPersistenceModel model)
    {
        return Order.Restore(
            model.Id,
            model.UserId,
            model.CourseId,
            model.AmountInCents,
            model.Currency,
            (OrderStatus)model.Status,
            model.StripeCheckoutSessionId,
            model.PaidAt,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static OrderPersistenceModel ToPersistence(Order order)
    {
        var model = new OrderPersistenceModel
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt
        };

        ApplyChanges(order, model);

        return model;
    }

    public static void ApplyChanges(Order order, OrderPersistenceModel model)
    {
        model.UserId = order.UserId;
        model.CourseId = order.CourseId;
        model.AmountInCents = order.AmountInCents;
        model.Currency = order.Currency;
        model.Status = (int)order.Status;
        model.StripeCheckoutSessionId = order.StripeCheckoutSessionId;
        model.PaidAt = order.PaidAt;
        model.UpdatedAt = order.UpdatedAt;
    }
}
