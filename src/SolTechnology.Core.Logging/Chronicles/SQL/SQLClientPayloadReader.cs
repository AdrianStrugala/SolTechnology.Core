using System.Data;

namespace SolTechnology.Core.Logging.Chronicles.SQL;

/// <summary>The fields Chronicle needs from a SqlClient command event.</summary>
internal readonly record struct SQLClientPayload(Guid OperationId, IDbCommand? Command, Exception? Exception);

/// <summary>
/// Reads SqlClient's command payloads (<c>SqlClientCommandBefore</c>/<c>After</c>/<c>Error</c>),
/// which are <see cref="IReadOnlyList{T}"/> of key/value pairs, without reflection and without a
/// <c>Microsoft.Data.SqlClient</c> reference: the command is read as <see cref="IDbCommand"/>.
/// </summary>
internal static class SQLClientPayloadReader
{
    internal static SQLClientPayload Read(object? payload)
    {
        if (payload is not IReadOnlyList<KeyValuePair<string, object>> pairs)
        {
            return default;
        }

        var (operationId, command, exception) = (Guid.Empty, (IDbCommand?)null, (Exception?)null);
        for (var i = 0; i < pairs.Count; i++)
        {
            switch (pairs[i].Key)
            {
                case "OperationId" when pairs[i].Value is Guid id:
                    operationId = id;
                    break;
                case "Command":
                    command = pairs[i].Value as IDbCommand;
                    break;
                case "Exception":
                    exception = pairs[i].Value as Exception;
                    break;
            }
        }

        return new SQLClientPayload(operationId, command, exception);
    }

    /// <summary>
    /// <c>SQL {procedure}</c> for a stored procedure, otherwise <c>SQL {first keyword}</c>, for example
    /// <c>SQL SELECT</c>. Leading comments (<c>--</c>, <c>/* */</c>, EF Core <c>TagWith</c>) and leading
    /// <c>SET …;</c> statements (EF Core <c>SaveChanges</c> batches) are skipped.
    /// </summary>
    internal static string StepName(IDbCommand command)
    {
        var text = command.CommandText ?? string.Empty;
        if (command.CommandType == CommandType.StoredProcedure)
        {
            return "SQL " + text.Trim();
        }

        var keyword = FirstKeyword(text.AsSpan());
        return keyword.IsEmpty ? "SQL" : "SQL " + keyword.ToString().ToUpperInvariant();
    }

    private static ReadOnlySpan<char> FirstKeyword(ReadOnlySpan<char> text)
    {
        while (true)
        {
            text = text.TrimStart();
            if (text.StartsWith("--"))
            {
                text = SkipPast(text, "\n");
                continue;
            }

            if (text.StartsWith("/*"))
            {
                text = SkipPast(text, "*/");
                continue;
            }

            var length = 0;
            while (length < text.Length && char.IsAsciiLetter(text[length]))
            {
                length++;
            }

            var keyword = text[..length];
            if (keyword.Equals("SET", StringComparison.OrdinalIgnoreCase) && text.IndexOf(';') is var end and >= 0)
            {
                text = text[(end + 1)..];
                continue;
            }

            return keyword;
        }
    }

    private static ReadOnlySpan<char> SkipPast(ReadOnlySpan<char> text, string terminator)
        => text.IndexOf(terminator.AsSpan()) is var at and >= 0 ? text[(at + terminator.Length)..] : [];
}
