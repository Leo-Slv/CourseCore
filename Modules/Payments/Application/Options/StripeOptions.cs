namespace CourseCore.Api.Modules.Payments.Application.Options;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Optional Stripe API base URL, only meant for local development against stripe-mock
    /// (e.g. http://localhost:12111). Rejected in Production.
    /// </summary>
    public string? ApiBase { get; set; }
}
