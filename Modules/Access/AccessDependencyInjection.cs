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
        services.AddScoped<IAccessRequestRepository, EfAccessRequestRepository>();
        services.AddScoped<CourseAccessService>();
        services.AddScoped<GrantUserAreaAccessUseCase>();
        services.AddScoped<GrantRoleAreaAccessUseCase>();
        services.AddScoped<CheckCourseAccessUseCase>();
        services.AddScoped<CreateAreaUseCase>();
        services.AddScoped<UpdateAreaUseCase>();
        services.AddScoped<GetAreaByIdUseCase>();
        services.AddScoped<ListAreasUseCase>();
        services.AddScoped<RequestCourseAccessUseCase>();
        services.AddScoped<ApproveAccessRequestUseCase>();
        services.AddScoped<RejectAccessRequestUseCase>();
        services.AddScoped<ListAccessRequestsUseCase>();
        services.AddScoped<ListMyAccessRequestsUseCase>();

        return services;
    }
}
