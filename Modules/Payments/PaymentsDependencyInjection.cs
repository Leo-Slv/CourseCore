using CourseCore.Api.Modules.Payments.Application.Contracts;
using CourseCore.Api.Modules.Payments.Application.Options;
using CourseCore.Api.Modules.Payments.Application.UseCases;
using CourseCore.Api.Modules.Payments.Domain.Repositories;
using CourseCore.Api.Modules.Payments.Infrastructure.Persistence.Repositories;
using CourseCore.Api.Modules.Payments.Infrastructure.Stripe;

namespace CourseCore.Api.Modules.Payments;

public static class PaymentsDependencyInjection
{
    public static IServiceCollection AddPaymentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IStripeGateway, StripeGateway>();
        services.AddScoped<IStripeWebhookVerifier, StripeWebhookVerifier>();
        services.AddScoped<CreateCheckoutUseCase>();
        services.AddScoped<HandleStripeWebhookUseCase>();
        services.AddScoped<ListMyOrdersUseCase>();

        return services;
    }
}
