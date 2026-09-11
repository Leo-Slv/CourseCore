using CourseCore.Api.Modules.Progress.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Progress.Application.UseCases;

public class RemoveLessonNoteUseCase
{
    private readonly ILessonNoteRepository _notes;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveLessonNoteUseCase(ILessonNoteRepository notes, IUnitOfWork unitOfWork)
    {
        _notes = notes;
        _unitOfWork = unitOfWork;
    }

    public Task ExecuteAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (lessonId == Guid.Empty)
        {
            throw new ArgumentException("LessonId is required.", nameof(lessonId));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var note = await _notes.FindByUserAndLessonAsync(userId, lessonId, cancellationToken);

            if (note is null)
            {
                throw new NotFoundException("No note is registered for this lesson.");
            }

            await _notes.RemoveAsync(userId, lessonId, cancellationToken);
        }, cancellationToken);
    }
}
