using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Repositories;

namespace CourseCore.Api.Modules.Users;

public static class UsersDependencyInjection
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, EfUserRepository>();

        return services;
    }
}
