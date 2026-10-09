using CourseCore.Api.Modules.Payments.Application.DTOs;
using CourseCore.Api.Modules.Payments.Presentation.Responses;

namespace CourseCore.Api.Modules.Payments.Presentation.Presenters;

public static class PaymentPresenter
{
    public static CheckoutResponse ToResponse(CheckoutOutput output)
    {
        return new CheckoutResponse
        {
            OrderId = output.OrderId,
            CheckoutUrl = output.CheckoutUrl
        };
    }

    public static OrderResponse ToResponse(OrderOutput output)
    {
        return new OrderResponse
        {
            Id = output.Id,
            CourseId = output.CourseId,
            AmountInCents = output.AmountInCents,
            Currency = output.Currency,
            Status = output.Status,
            PaidAt = output.PaidAt,
            CreatedAt = output.CreatedAt
        };
    }
}
