using CourseCore.Api.Modules.Questions.Application.UseCases;
using CourseCore.Api.Modules.Questions.Domain.Repositories;
using CourseCore.Api.Modules.Questions.Infrastructure.Persistence.Repositories;

namespace CourseCore.Api.Modules.Questions;

public static class QuestionsDependencyInjection
{
    public static IServiceCollection AddQuestionsModule(this IServiceCollection services)
    {
        services.AddScoped<ILessonQuestionRepository, EfLessonQuestionRepository>();
        services.AddScoped<AskLessonQuestionUseCase>();
        services.AddScoped<ListLessonQuestionsUseCase>();
        services.AddScoped<AnswerLessonQuestionUseCase>();
        services.AddScoped<RemoveLessonQuestionUseCase>();

        return services;
    }
}
