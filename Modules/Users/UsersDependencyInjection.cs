using CourseCore.Api.Modules.Users.Application.UseCases;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Repositories;

namespace CourseCore.Api.Modules.Users;

public static class UsersDependencyInjection
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<CreateUserUseCase>();
        services.AddScoped<UpdateUserUseCase>();
        services.AddScoped<ListUsersUseCase>();

        return services;
    }
}
