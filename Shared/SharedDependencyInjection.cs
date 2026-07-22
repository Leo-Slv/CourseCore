using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using CourseCore.Api.Shared.Infrastructure.Persistence.Seed;
using CourseCore.Api.Shared.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Shared;

public static class SharedDependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CourseCoreDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'CourseCoreDatabase' was not found.");
        }

        services.AddDbContext<CourseCoreDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.Configure<AdminSeedOptions>(
            configuration.GetSection(AdminSeedOptions.SectionName));
        services.AddScoped<CourseCoreDatabaseSeeder>();

        return services;
    }
}
