using CourseCore.Api.Modules.Visitors.Application.UseCases;
using CourseCore.Api.Modules.Visitors.Domain.Repositories;
using CourseCore.Api.Modules.Visitors.Infrastructure.Persistence.Repositories;

namespace CourseCore.Api.Modules.Visitors;

public static class VisitorsDependencyInjection
{
    public static IServiceCollection AddVisitorsModule(this IServiceCollection services)
    {
        services.AddScoped<IVisitorRepository, EfVisitorRepository>();
        services.AddScoped<RegisterVisitorUseCase>();
        services.AddScoped<ListVisitorsUseCase>();

        return services;
    }
}
