using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.Validation;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class UpdateAreaUseCase
{
    private readonly IAreaRepository _areas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public UpdateAreaUseCase(
        IAreaRepository areas,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _areas = areas;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<AreaOutput> ExecuteAsync(
        UpdateAreaInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.AreaId == Guid.Empty)
        {
            throw new ArgumentException("AreaId is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > AreaValidationLimits.NameMaxLength)
        {
            throw new ApplicationValidationException("Name is invalid.");
        }

        if ((input.Description ?? string.Empty).Trim().Length > AreaValidationLimits.DescriptionMaxLength)
        {
            throw new ApplicationValidationException("Description is invalid.");
        }

        if (input.Slug.Trim().Length > AreaValidationLimits.SlugMaxLength)
        {
            throw new ApplicationValidationException("Slug is invalid.");
        }

        var slug = Slug.Create(input.Slug);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var area = await _areas.FindByIdAsync(input.AreaId, cancellationToken);

            if (area is null)
            {
                throw new NotFoundException("Area not found.");
            }

            if (area.Slug != slug && await _areas.FindBySlugAsync(slug, cancellationToken) is not null)
            {
                throw new ConflictException("An area with this slug already exists.");
            }

            var requestedName = input.Name.Trim();
            var requestedDescription = (input.Description ?? string.Empty).Trim();

            if (!string.Equals(area.Name, requestedName, StringComparison.Ordinal))
            {
                area.ChangeName(requestedName);
            }

            if (area.Slug != slug)
            {
                area.ChangeSlug(slug);
            }

            if (!string.Equals(area.Description, requestedDescription, StringComparison.Ordinal))
            {
                area.ChangeDescription(requestedDescription);
            }

            if (area.DisplayOrder != input.DisplayOrder)
            {
                area.ChangeDisplayOrder(input.DisplayOrder);
            }

            var activeChanged = area.Active != input.Active;

            if (activeChanged && input.Active)
            {
                area.Activate();
            }
            else if (activeChanged)
            {
                area.Deactivate();
            }

            await _areas.UpdateAsync(area, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.AreaUpdated,
                "Area",
                area.Id,
                new Dictionary<string, string?> { ["slug"] = area.Slug.Value },
                cancellationToken: cancellationToken);

            if (activeChanged)
            {
                await _auditLogs.RecordAsync(
                    area.Active ? AuditLogActionNames.AreaActivated : AuditLogActionNames.AreaDeactivated,
                    "Area",
                    area.Id,
                    cancellationToken: cancellationToken);
            }

            return AreaOutput.FromArea(area);
        }, cancellationToken);
    }
}
