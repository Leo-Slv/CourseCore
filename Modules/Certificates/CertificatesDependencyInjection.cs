using CourseCore.Api.Modules.Certificates.Application.UseCases;
using CourseCore.Api.Modules.Certificates.Domain.Repositories;
using CourseCore.Api.Modules.Certificates.Infrastructure.Persistence.Repositories;

namespace CourseCore.Api.Modules.Certificates;

public static class CertificatesDependencyInjection
{
    public static IServiceCollection AddCertificatesModule(this IServiceCollection services)
    {
        services.AddScoped<ICertificateRepository, EfCertificateRepository>();
        services.AddScoped<ListMyCertificatesUseCase>();

        return services;
    }
}
