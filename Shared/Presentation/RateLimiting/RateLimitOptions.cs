namespace CourseCore.Api.Shared.Presentation.RateLimiting;

public sealed class RateLimitOptions
{
    /// <summary>Safety net for every request, per signed-in user (or per IP when anonymous).</summary>
    public RateLimitPolicyOptions Global { get; init; } = new()
    {
        PermitLimit = 300,
        WindowSeconds = 60
    };

    /// <summary>Creating a Stripe Checkout session (costs a call to Stripe).</summary>
    public RateLimitPolicyOptions Checkout { get; init; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60
    };

    /// <summary>Requests for pre-signed upload URLs.</summary>
    public RateLimitPolicyOptions Upload { get; init; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60
    };

    /// <summary>Content a student posts: questions, testimonials, access requests.</summary>
    public RateLimitPolicyOptions UserContent { get; init; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions Login { get; init; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions Refresh { get; init; } = new()
    {
        PermitLimit = 20,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions Logout { get; init; } = new()
    {
        PermitLimit = 30,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions Register { get; init; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions ResendConfirmation { get; init; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions ForgotPassword { get; init; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions ResetPassword { get; init; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60
    };

    public RateLimitPolicyOptions ChangePassword { get; init; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60
    };

    // Looser than account registration: a reception kiosk submits many visitors from one IP.
    public RateLimitPolicyOptions VisitorRegistration { get; init; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60
    };
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; init; } = 10;

    public int WindowSeconds { get; init; } = 60;

    public int QueueLimit { get; init; }
}
