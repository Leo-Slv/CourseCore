namespace CourseCore.Api.Modules.Access.Presentation.Requests;

public class CreateAreaRequest
{
    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}
