namespace CourseCore.Api.Shared.Infrastructure.Persistence.Seed;

public sealed class AdminSeedOptions
{
    public const string SectionName = "Seed:Admin";

    public bool Enabled { get; init; }

    public string Name { get; init; } = "CourseCore Admin";

    public string Email { get; init; } = "admin@coursecore.local";

    public string? Password { get; init; }

    public bool ResetPassword { get; init; }
}
