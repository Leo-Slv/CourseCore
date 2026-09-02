using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Infrastructure.Email;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using CourseCore.Api.Shared.Infrastructure.Persistence.Seed;
using CourseCore.Api.Shared.Infrastructure.Security;
using CourseCore.Api.Shared.Presentation.Observability;
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
        services.AddScoped<IRequestContextService, RequestContextService>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.Configure<AdminSeedOptions>(
            configuration.GetSection(AdminSeedOptions.SectionName));
        services.AddScoped<CourseCoreDatabaseSeeder>();

        services.Configure<ResendOptions>(configuration.GetSection("Resend"));
        services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
        });

        return services;
    }
}
