using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Testimonials.Application.DTOs;
using CourseCore.Api.Modules.Testimonials.Application.Validation;
using CourseCore.Api.Modules.Testimonials.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Testimonials.Application.UseCases;

public class UpdateTestimonialUseCase
{
    private readonly ITestimonialRepository _testimonials;
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public UpdateTestimonialUseCase(
        ITestimonialRepository testimonials,
        ICourseRepository courses,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _testimonials = testimonials;
        _courses = courses;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<TestimonialOutput> ExecuteAsync(
        UpdateTestimonialInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.TestimonialId == Guid.Empty)
        {
            throw new ArgumentException("TestimonialId is required.", nameof(input));
        }

        TestimonialInputValidator.Validate(input.AuthorName, input.Quote, input.AvatarUrl);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var testimonial = await _testimonials.FindByIdAsync(input.TestimonialId, cancellationToken);

            if (testimonial is null)
            {
                throw new NotFoundException("Testimonial not found.");
            }

            if (input.CourseId is { } courseId && await _courses.FindByIdAsync(courseId, cancellationToken) is null)
            {
                throw new NotFoundException("Course not found.");
            }

            testimonial.ChangeContent(input.AuthorName, input.Quote, input.AvatarUrl, input.CourseId);

            await _testimonials.UpdateAsync(testimonial, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.TestimonialUpdated,
                "Testimonial",
                testimonial.Id,
                cancellationToken: cancellationToken);

            return TestimonialOutput.FromTestimonial(testimonial);
        }, cancellationToken);
    }
}
