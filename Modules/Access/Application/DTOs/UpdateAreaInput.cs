namespace CourseCore.Api.Modules.Access.Application.DTOs;

public class UpdateAreaInput
{
    public Guid AreaId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool Active { get; init; }
}
