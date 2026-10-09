using CourseCore.Api.Modules.Payments.Domain.Entities;

namespace CourseCore.Api.Modules.Payments.Application.DTOs;

public class OrderOutput
{
    public Guid Id { get; init; }

    public Guid CourseId { get; init; }

    public long AmountInCents { get; init; }

    public string Currency { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime? PaidAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public static OrderOutput FromOrder(Order order)
    {
        return new OrderOutput
        {
            Id = order.Id,
            CourseId = order.CourseId,
            AmountInCents = order.AmountInCents,
            Currency = order.Currency,
            Status = order.Status.ToString(),
            PaidAt = order.PaidAt,
            CreatedAt = order.CreatedAt
        };
    }
}
