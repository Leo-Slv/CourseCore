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
        services.AddScoped<ICourseModuleRepository, EfCourseModuleRepository>();
        services.AddScoped<CreateCourseUseCase>();
        services.AddScoped<UpdateCourseUseCase>();
        services.AddScoped<PublishCourseUseCase>();
        services.AddScoped<UnpublishCourseUseCase>();
        services.AddScoped<GetCourseDetailsUseCase>();
        services.AddScoped<ListAvailableCoursesUseCase>();
        services.AddScoped<ListAllCoursesUseCase>();
        services.AddScoped<GetPublicCatalogSummaryUseCase>();
        services.AddScoped<CreateCourseModuleUseCase>();
        services.AddScoped<UpdateCourseModuleUseCase>();
        services.AddScoped<RemoveCourseModuleUseCase>();
        services.AddScoped<ReorderCourseModulesUseCase>();
        services.AddScoped<CreateLessonUseCase>();
        services.AddScoped<UpdateLessonUseCase>();
        services.AddScoped<RemoveLessonUseCase>();
        services.AddScoped<ReorderLessonsUseCase>();

        return services;
    }
}
