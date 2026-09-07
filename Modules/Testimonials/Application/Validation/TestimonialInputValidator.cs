using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Testimonials.Application.Validation;

public static class TestimonialInputValidator
{
    public static void Validate(string authorName, string quote, string? avatarUrl)
    {
        if (!IsValidRequired(authorName, TestimonialValidationLimits.AuthorNameMaxLength)
            || !IsValidRequired(quote, TestimonialValidationLimits.QuoteMaxLength)
            || !IsValidHttpUrl(avatarUrl, TestimonialValidationLimits.AvatarUrlMaxLength))
        {
            throw new ApplicationValidationException("Testimonial payload is invalid.");
        }
    }

    private static bool IsValidRequired(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength;

    private static bool IsValidHttpUrl(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            && Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https";
    }
}
