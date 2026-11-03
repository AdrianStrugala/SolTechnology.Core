using Microsoft.AspNetCore.Http;

namespace SolTechnology.Core.Logging.Middleware;

/// <summary>
/// Matches a request path against configured path prefixes per segment (case-insensitive):
/// <c>/health</c> matches <c>/health</c> and <c>/health/ready</c>, not <c>/healthcare</c>.
/// </summary>
internal static class LoggingPathMatcher
{
    public static bool MatchesAny(PathString path, IEnumerable<string>? prefixes)
    {
        if (prefixes is null || !path.HasValue)
        {
            return false;
        }

        foreach (var prefix in prefixes)
        {
            if (Matches(path, prefix))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Matches(PathString path, string? prefix)
    {
        // Request paths always start with '/', so a prefix without it can never match.
        if (string.IsNullOrEmpty(prefix) || prefix[0] != '/')
        {
            return false;
        }

        return path.StartsWithSegments(new PathString(prefix.TrimEnd('/')), StringComparison.OrdinalIgnoreCase);
    }
}
