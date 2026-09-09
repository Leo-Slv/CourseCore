using CourseCore.Api.Modules.Testimonials.Application.UseCases;
using CourseCore.Api.Modules.Testimonials.Domain.Repositories;
using CourseCore.Api.Modules.Testimonials.Infrastructure.Persistence.Repositories;

namespace CourseCore.Api.Modules.Testimonials;

public static class TestimonialsDependencyInjection
{
    public static IServiceCollection AddTestimonialsModule(this IServiceCollection services)
    {
        services.AddScoped<ITestimonialRepository, EfTestimonialRepository>();
        services.AddScoped<CreateTestimonialUseCase>();
        services.AddScoped<UpdateTestimonialUseCase>();
        services.AddScoped<PublishTestimonialUseCase>();
        services.AddScoped<UnpublishTestimonialUseCase>();
        services.AddScoped<ListTestimonialsUseCase>();
        services.AddScoped<ListPublicTestimonialsUseCase>();
        services.AddScoped<SubmitTestimonialUseCase>();

        return services;
    }
}
