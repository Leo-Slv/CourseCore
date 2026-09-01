using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.Validation;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class CreateAreaUseCase
{
    private readonly IAreaRepository _areas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public CreateAreaUseCase(
        IAreaRepository areas,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _areas = areas;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<AreaOutput> ExecuteAsync(
        CreateAreaInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > AreaValidationLimits.NameMaxLength)
        {
            throw new ApplicationValidationException("Name is invalid.");
        }

        var description = (input.Description ?? string.Empty).Trim();

        if (description.Length > AreaValidationLimits.DescriptionMaxLength)
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
            if (await _areas.FindBySlugAsync(slug, cancellationToken) is not null)
            {
                throw new ConflictException("An area with this slug already exists.");
            }

            var area = Area.Create(input.Name, slug, description, input.DisplayOrder);

            await _areas.CreateAsync(area, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.AreaCreated,
                "Area",
                area.Id,
                new Dictionary<string, string?> { ["slug"] = area.Slug.Value },
                cancellationToken: cancellationToken);

            return AreaOutput.FromArea(area);
        }, cancellationToken);
    }
}
