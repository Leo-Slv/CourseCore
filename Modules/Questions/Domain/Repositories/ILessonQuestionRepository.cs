using CourseCore.Api.Modules.Questions.Domain.Entities;

namespace CourseCore.Api.Modules.Questions.Domain.Repositories;

public interface ILessonQuestionRepository
{
    Task<LessonQuestion?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LessonQuestion>> ListByLessonIdAsync(
        Guid lessonId,
        CancellationToken cancellationToken = default);

    Task CreateAsync(LessonQuestion question, CancellationToken cancellationToken = default);

    Task UpdateAsync(LessonQuestion question, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid questionId, CancellationToken cancellationToken = default);
}
