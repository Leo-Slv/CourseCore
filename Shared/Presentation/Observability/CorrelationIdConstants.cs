namespace CourseCore.Api.Shared.Presentation.Observability;

public static class CorrelationIdConstants
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemName = "CorrelationId";
    public const int MaxLength = 128;

    public static string? GetFromItems(HttpContext context)
    {
        return context.Items.TryGetValue(ItemName, out var value)
            ? value as string
            : null;
    }
}
