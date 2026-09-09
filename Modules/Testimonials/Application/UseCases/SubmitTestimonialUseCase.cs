using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Testimonials.Application.DTOs;
using CourseCore.Api.Modules.Testimonials.Application.Validation;
using CourseCore.Api.Modules.Testimonials.Domain.Entities;
using CourseCore.Api.Modules.Testimonials.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Testimonials.Application.UseCases;

public class SubmitTestimonialUseCase
{
    private readonly IUserRepository _users;
    private readonly ICourseRepository _courses;
    private readonly ITestimonialRepository _testimonials;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public SubmitTestimonialUseCase(
        IUserRepository users,
        ICourseRepository courses,
        ITestimonialRepository testimonials,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _courses = courses;
        _testimonials = testimonials;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<TestimonialOutput> ExecuteAsync(
        SubmitTestimonialInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.Quote) || input.Quote.Trim().Length > TestimonialValidationLimits.QuoteMaxLength)
        {
            throw new ApplicationValidationException("Quote is invalid.");
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _users.FindByIdAsync(input.UserId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            if (input.CourseId is { } courseId && await _courses.FindByIdAsync(courseId, cancellationToken) is null)
            {
                throw new NotFoundException("Course not found.");
            }

            var testimonial = Testimonial.Create(
                user.Name,
                input.Quote,
                user.AvatarUrl,
                input.CourseId,
                submittedByUserId: user.Id);

            await _testimonials.CreateAsync(testimonial, cancellationToken);

            var metadata = new Dictionary<string, string?>
            {
                ["displayName"] = user.Name,
                ["submittedByUserId"] = user.Id.ToString()
            };

            if (input.CourseId is { } linkedCourseId)
            {
                metadata["courseId"] = linkedCourseId.ToString();
            }

            await _auditLogs.RecordAsync(
                AuditLogActionNames.TestimonialSubmitted,
                "Testimonial",
                testimonial.Id,
                metadata,
                userId: user.Id,
                cancellationToken: cancellationToken);

            return TestimonialOutput.FromTestimonial(testimonial);
        }, cancellationToken);
    }
}
