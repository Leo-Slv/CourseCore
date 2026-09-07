namespace CourseCore.Api.Modules.Auth.Application.Services;

public static class AuthEmailTemplates
{
    public static string BuildEmailConfirmationHtml(string baseUrl, string token)
    {
        var link = BuildLink(baseUrl, "confirm-email", token);

        return $"<p>Use o link a seguir para confirmar seu e-mail:</p>"
            + $"<p><a href=\"{link}\">Confirmar e-mail</a></p>"
            + $"<p>Ou use o código a seguir, se preferir:</p>"
            + $"<p><strong>{token}</strong></p>";
    }

    public static string BuildPasswordResetHtml(string baseUrl, string token)
    {
        var link = BuildLink(baseUrl, "reset-password", token);

        return $"<p>Use o link a seguir para redefinir sua senha:</p>"
            + $"<p><a href=\"{link}\">Redefinir senha</a></p>"
            + $"<p>Ou use o código a seguir, se preferir:</p>"
            + $"<p><strong>{token}</strong></p>";
    }

    private static string BuildLink(string baseUrl, string path, string token)
    {
        return $"{baseUrl.TrimEnd('/')}/{path}?token={Uri.EscapeDataString(token)}";
    }
}
