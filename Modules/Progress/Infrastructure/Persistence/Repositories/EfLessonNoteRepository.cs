using CourseCore.Api.Modules.Progress.Domain.Entities;
using CourseCore.Api.Modules.Progress.Domain.Repositories;
using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Repositories;

public class EfLessonNoteRepository : ILessonNoteRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfLessonNoteRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LessonNote?> FindByUserAndLessonAsync(
        Guid userId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.LessonNotes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.LessonId == lessonId, cancellationToken);

        return model is null ? null : LessonNoteMapper.ToDomain(model);
    }

    public async Task SaveAsync(LessonNote note, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.LessonNotes
            .FirstOrDefaultAsync(x => x.UserId == note.UserId && x.LessonId == note.LessonId, cancellationToken);

        if (model is null)
        {
            await _dbContext.LessonNotes.AddAsync(LessonNoteMapper.ToPersistence(note), cancellationToken);
            return;
        }

        LessonNoteMapper.ApplyChanges(note, model);
    }

    public async Task RemoveAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.LessonNotes
            .FirstOrDefaultAsync(x => x.UserId == userId && x.LessonId == lessonId, cancellationToken);

        if (model is not null)
        {
            _dbContext.LessonNotes.Remove(model);
        }
    }
}
