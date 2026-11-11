using System.Net.Http.Headers;

namespace ManagementPanel.Api.Codex;

internal static class CodexBearerKey
{
    public static string? ReadHash(HttpRequest request)
    {
        if (!AuthenticationHeaderValue.TryParse(request.Headers.Authorization.ToString(), out var header) ||
            !header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) ||
            header.Parameter is not { Length: 68 } token ||
            !token.StartsWith("mpk_", StringComparison.Ordinal))
            return null;

        return CodexKeyEndpoints.HashToken(token);
    }
}
