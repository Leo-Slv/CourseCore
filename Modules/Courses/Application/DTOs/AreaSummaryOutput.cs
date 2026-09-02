using CourseCore.Api.Modules.Access.Application.DTOs;

namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class AreaSummaryOutput
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public static AreaSummaryOutput FromAreaOutput(AreaOutput area)
    {
        return new AreaSummaryOutput
        {
            Id = area.Id,
            Name = area.Name,
            Slug = area.Slug,
            DisplayOrder = area.DisplayOrder
        };
    }
}
