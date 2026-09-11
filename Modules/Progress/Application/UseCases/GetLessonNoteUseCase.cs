using CourseCore.Api.Modules.Progress.Application.DTOs;
using CourseCore.Api.Modules.Progress.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Progress.Application.UseCases;

public class GetLessonNoteUseCase
{
    private readonly ILessonNoteRepository _notes;

    public GetLessonNoteUseCase(ILessonNoteRepository notes)
    {
        _notes = notes;
    }

    public async Task<LessonNoteOutput> ExecuteAsync(
        Guid userId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (lessonId == Guid.Empty)
        {
            throw new ArgumentException("LessonId is required.", nameof(lessonId));
        }

        var note = await _notes.FindByUserAndLessonAsync(userId, lessonId, cancellationToken);

        if (note is null)
        {
            throw new NotFoundException("No note is registered for this lesson.");
        }

        return LessonNoteOutput.FromNote(note);
    }
}
