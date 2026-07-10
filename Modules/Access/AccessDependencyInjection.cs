using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Repositories;

namespace CourseCore.Api.Modules.Access;

public static class AccessDependencyInjection
{
    public static IServiceCollection AddAccessModule(this IServiceCollection services)
    {
        services.AddScoped<IRoleRepository, EfRoleRepository>();
        services.AddScoped<IAreaRepository, EfAreaRepository>();
        services.AddScoped<CourseAccessService>();
        services.AddScoped<GrantUserAreaAccessUseCase>();
        services.AddScoped<GrantRoleAreaAccessUseCase>();
        services.AddScoped<CheckCourseAccessUseCase>();

        return services;
    }
}
