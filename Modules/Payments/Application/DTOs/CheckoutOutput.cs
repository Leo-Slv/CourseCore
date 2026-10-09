namespace CourseCore.Api.Modules.Payments.Application.DTOs;

public class CheckoutOutput
{
    public Guid OrderId { get; init; }

    public string CheckoutUrl { get; init; } = string.Empty;
}
