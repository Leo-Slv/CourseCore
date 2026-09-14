using System.Text;

namespace CourseCore.Api.Modules.Media.Application.Services;

public static class StorageKeyGenerator
{
    private const int MaxExtensionLength = 10;

    public static string Generate(string prefix, Guid lessonId, string fileName)
    {
        var extension = ExtractSafeExtension(fileName);

        return $"{prefix}/{lessonId:N}/{Guid.NewGuid():N}{extension}";
    }

    private static string ExtractSafeExtension(string fileName)
    {
        var dotIndex = fileName.LastIndexOf('.');

        if (dotIndex < 0 || dotIndex == fileName.Length - 1)
        {
            return string.Empty;
        }

        var rawExtension = fileName[(dotIndex + 1)..];
        var builder = new StringBuilder();

        foreach (var character in rawExtension)
        {
            if (builder.Length >= MaxExtensionLength)
            {
                break;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.Length == 0 ? string.Empty : $".{builder}";
    }
}
