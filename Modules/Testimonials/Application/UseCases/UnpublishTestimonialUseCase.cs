using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Testimonials.Application.DTOs;
using CourseCore.Api.Modules.Testimonials.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Testimonials.Application.UseCases;

public class UnpublishTestimonialUseCase
{
    private readonly ITestimonialRepository _testimonials;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public UnpublishTestimonialUseCase(
        ITestimonialRepository testimonials,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _testimonials = testimonials;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<TestimonialOutput> ExecuteAsync(Guid testimonialId, CancellationToken cancellationToken = default)
    {
        return _unitOfWork.ExecuteAsync(async () =>
        {
            var testimonial = await _testimonials.FindByIdAsync(testimonialId, cancellationToken);

            if (testimonial is null)
            {
                throw new NotFoundException("Testimonial not found.");
            }

            testimonial.Unpublish();
            await _testimonials.UpdateAsync(testimonial, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.TestimonialUnpublished,
                "Testimonial",
                testimonial.Id,
                cancellationToken: cancellationToken);

            return TestimonialOutput.FromTestimonial(testimonial);
        }, cancellationToken);
    }
}
