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
        services.AddScoped<IUserCourseAccessRepository, EfUserCourseAccessRepository>();
        services.AddScoped<UserCourseAccessGranter>();
        services.AddScoped<CourseAccessService>();
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
        services.AddScoped<ListUserCourseAccessUseCase>();
        services.AddScoped<RevokeUserCourseAccessUseCase>();
        services.AddScoped<GrantCourseAccessUseCase>();
        services.AddScoped<ListGrantedCourseAccessUseCase>();
        services.AddScoped<IPermissionRepository, EfPermissionRepository>();
        services.AddScoped<ListRolesUseCase>();
        services.AddScoped<ListRoleDetailsUseCase>();
        services.AddScoped<ListPermissionsUseCase>();
        services.AddScoped<CreateRoleUseCase>();
        services.AddScoped<UpdateRoleUseCase>();
        services.AddScoped<SetRolePermissionsUseCase>();
        services.AddScoped<RequestAreaImageUploadUseCase>();

        return services;
    }
}
