namespace CourseCore.Api.Shared.Infrastructure.Persistence.Seed;

public static class CourseCoreDatabaseSeedExtensions
{
    public static async Task SeedCourseCoreDatabaseAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<CourseCoreDatabaseSeeder>();

        await seeder.SeedAsync(cancellationToken);
    }
}
