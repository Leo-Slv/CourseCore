namespace CourseCore.Api.Modules.Access.Presentation.Requests;

public class CreateAreaRequest
{
    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public string AccentColor { get; init; } = "Blue";

    public string? ImageUrl { get; init; }

    public bool IsPublic { get; init; }
}
