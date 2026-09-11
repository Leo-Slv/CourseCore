using CourseCore.Api.Modules.Questions.Domain.Entities;
using CourseCore.Api.Modules.Questions.Domain.Repositories;
using CourseCore.Api.Modules.Questions.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Questions.Infrastructure.Persistence.Repositories;

public class EfLessonQuestionRepository : ILessonQuestionRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfLessonQuestionRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LessonQuestion?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.LessonQuestions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : LessonQuestionMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<LessonQuestion>> ListByLessonIdAsync(
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.LessonQuestions
            .AsNoTracking()
            .Where(x => x.LessonId == lessonId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return models.Select(LessonQuestionMapper.ToDomain).ToList();
    }

    public async Task CreateAsync(LessonQuestion question, CancellationToken cancellationToken = default)
    {
        await _dbContext.LessonQuestions.AddAsync(LessonQuestionMapper.ToPersistence(question), cancellationToken);
    }

    public async Task UpdateAsync(LessonQuestion question, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.LessonQuestions
            .FirstOrDefaultAsync(x => x.Id == question.Id, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Lesson question not found.");
        }

        LessonQuestionMapper.ApplyChanges(question, model);
    }

    public async Task RemoveAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.LessonQuestions
            .FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Lesson question not found.");
        }

        _dbContext.LessonQuestions.Remove(model);
    }
}
