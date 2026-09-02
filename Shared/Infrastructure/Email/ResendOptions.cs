namespace CourseCore.Api.Shared.Infrastructure.Email;

public sealed class ResendOptions
{
    public string ApiKey { get; init; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = string.Empty;
}
