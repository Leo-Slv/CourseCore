using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Progress.Application.DTOs;
using CourseCore.Api.Modules.Progress.Application.Validation;
using CourseCore.Api.Modules.Progress.Domain.Entities;
using CourseCore.Api.Modules.Progress.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Progress.Application.UseCases;

public class UpsertLessonNoteUseCase
{
    private readonly IUserRepository _users;
    private readonly ILessonRepository _lessons;
    private readonly ICourseRepository _courses;
    private readonly ILessonNoteRepository _notes;
    private readonly CourseAccessService _courseAccessService;
    private readonly IUnitOfWork _unitOfWork;

    public UpsertLessonNoteUseCase(
        IUserRepository users,
        ILessonRepository lessons,
        ICourseRepository courses,
        ILessonNoteRepository notes,
        CourseAccessService courseAccessService,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _lessons = lessons;
        _courses = courses;
        _notes = notes;
        _courseAccessService = courseAccessService;
        _unitOfWork = unitOfWork;
    }

    public Task<LessonNoteOutput> ExecuteAsync(
        UpsertLessonNoteInput input,
        CancellationToken cancellationToken = default)
    {
        LessonNoteInputValidator.Validate(input);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _users.FindByIdAsync(input.UserId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            if (!user.Active)
            {
                throw new ForbiddenException("User is inactive.");
            }

            var lesson = await _lessons.FindByIdAsync(input.LessonId, cancellationToken);

            if (lesson is null)
            {
                throw new NotFoundException("Lesson not found.");
            }

            var course = await _courses.FindByLessonIdAsync(lesson.Id, cancellationToken);

            if (course is null)
            {
                throw new NotFoundException("Course not found for lesson.");
            }

            var access = await _courseAccessService.CanUserAccessCourseAsync(
                input.UserId,
                course.Id,
                cancellationToken);

            if (!access.CanAccess)
            {
                throw new ForbiddenException("User cannot access this course.");
            }

            var note = await _notes.FindByUserAndLessonAsync(input.UserId, input.LessonId, cancellationToken)
                ?? LessonNote.Create(input.UserId, input.LessonId, input.Content);

            note.ChangeContent(input.Content);

            await _notes.SaveAsync(note, cancellationToken);

            return LessonNoteOutput.FromNote(note);
        }, cancellationToken);
    }
}
