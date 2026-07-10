using CourseCore.Api.Modules.Courses.Application.UseCases;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Repositories;

namespace CourseCore.Api.Modules.Courses;

public static class CoursesDependencyInjection
{
    public static IServiceCollection AddCoursesModule(this IServiceCollection services)
    {
        services.AddScoped<ICourseRepository, EfCourseRepository>();
        services.AddScoped<ILessonRepository, EfLessonRepository>();
        services.AddScoped<CreateCourseUseCase>();
        services.AddScoped<UpdateCourseUseCase>();
        services.AddScoped<PublishCourseUseCase>();
        services.AddScoped<GetCourseDetailsUseCase>();
        services.AddScoped<ListAvailableCoursesUseCase>();

        return services;
    }
}
