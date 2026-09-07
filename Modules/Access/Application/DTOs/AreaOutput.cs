using CourseCore.Api.Modules.Access.Domain.Entities;

namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class AreaOutput
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool Active { get; init; }

    public int DisplayOrder { get; init; }

    public string AccentColor { get; init; } = string.Empty;

    public int CourseCount { get; init; }

    public IReadOnlyCollection<AreaCourseSummaryOutput> Courses { get; init; } = Array.Empty<AreaCourseSummaryOutput>();

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public static AreaOutput FromArea(
        Area area,
        int courseCount = 0,
        IReadOnlyCollection<AreaCourseSummaryOutput>? courses = null)
    {
        return new AreaOutput
        {
            Id = area.Id,
            Name = area.Name,
            Slug = area.Slug.Value,
            Description = area.Description,
            Active = area.Active,
            DisplayOrder = area.DisplayOrder,
            AccentColor = area.AccentColor.ToString(),
            CourseCount = courseCount,
            Courses = courses ?? Array.Empty<AreaCourseSummaryOutput>(),
            CreatedAt = area.CreatedAt,
            UpdatedAt = area.UpdatedAt
        };
    }
}
