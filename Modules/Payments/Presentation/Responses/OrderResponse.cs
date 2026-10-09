namespace CourseCore.Api.Modules.Payments.Presentation.Responses;

public class OrderResponse
{
    public Guid Id { get; init; }

    public Guid CourseId { get; init; }

    public long AmountInCents { get; init; }

    public string Currency { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime? PaidAt { get; init; }

    public DateTime CreatedAt { get; init; }
}
