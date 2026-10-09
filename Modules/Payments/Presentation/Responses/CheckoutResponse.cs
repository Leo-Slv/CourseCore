namespace CourseCore.Api.Modules.Payments.Presentation.Responses;

public class CheckoutResponse
{
    public Guid OrderId { get; init; }

    public string CheckoutUrl { get; init; } = string.Empty;
}
