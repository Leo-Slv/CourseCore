namespace CourseCore.Api.Shared.Presentation.RateLimiting;

public sealed class RateLimitOptions
{
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
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; init; } = 10;

    public int WindowSeconds { get; init; } = 60;

    public int QueueLimit { get; init; }
}
