using System.Buffers;
using System.Data;

namespace SolTechnology.Core.Logging.Chronicles.SQL;

/// <summary>
/// Names a SqlClient command step <c>SQL {OPERATION} {target}</c>, after the OpenTelemetry span name
/// <c>{db.operation.name} {db.collection.name}</c>: <c>SQL SELECT Cities</c>, <c>SQL INSERT HangFire.Job</c>,
/// <c>SQL EXEC dbo.ImportCities</c>. Leading comments, <c>SET …;</c>, <c>DECLARE …;</c> and
/// <c>BEGIN TRAN</c> are skipped; the target keeps at most the last two name parts without quotes.
/// Without a target the name is <c>SQL {OPERATION}</c>, without an operation <c>SQL</c>. Only identifiers
/// are used, never literals; the first 4096 characters are scanned and only the name is allocated.
/// </summary>
internal static class SQLStepNamer
{
    private const int MaxScannedLength = 4096;
    // Bounds the name buffer; a longer first word is not a T-SQL keyword.
    private const int MaxOperationLength = 32;
    private const int MaxTargetLength = 128;
    private static readonly SearchValues<char> AsciiLetters = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

    internal static string Name(string? commandText, CommandType commandType)
    {
        var text = commandText.AsSpan();
        var tokens = new SQLTokenizer(text[..Math.Min(text.Length, MaxScannedLength)]);
        if (commandType == CommandType.StoredProcedure)
        {
            return Compose("EXEC", tokens.Next());
        }

        var operation = SkipPreamble(ref tokens);
        return Compose(operation, TargetOf(operation, ref tokens));
    }

    private static ReadOnlySpan<char> SkipPreamble(ref SQLTokenizer tokens)
    {
        while (true)
        {
            var keyword = tokens.Next();
            var rest = tokens;
            var isNoise = keyword is ";"
                || ((Is(keyword, "SET") || Is(keyword, "DECLARE")) && rest.SkipPastSemicolon())
                || (Is(keyword, "BEGIN") && rest.Next() is var next && (Is(next, "TRAN") || Is(next, "TRANSACTION")));
            if (!isNoise)
            {
                return keyword;
            }

            tokens = rest;
        }
    }

    private static ReadOnlySpan<char> TargetOf(scoped ReadOnlySpan<char> operation, ref SQLTokenizer tokens)
        => Is(operation, "SELECT") ? FirstSource(ref tokens)
            : Is(operation, "INSERT") || Is(operation, "MERGE") ? AfterOptional("INTO", ref tokens)
            : Is(operation, "DELETE") ? AfterOptional("FROM", ref tokens)
            : Is(operation, "EXEC") || Is(operation, "EXECUTE") ? Procedure(ref tokens)
            : Is(operation, "UPDATE") || (Is(operation, "TRUNCATE") && Is(tokens.Next(), "TABLE")) ? tokens.Next()
            : [];

    // The first FROM followed by a name; FROM ( opens a subquery whose own FROM is the source.
    private static ReadOnlySpan<char> FirstSource(ref SQLTokenizer tokens)
    {
        for (var token = tokens.Next(); !token.IsEmpty && token is not ";"; token = tokens.Next())
        {
            if (Is(token, "FROM") && tokens.Next() is var source && source is not "(")
            {
                return source;
            }
        }

        return [];
    }

    private static ReadOnlySpan<char> AfterOptional(string keyword, ref SQLTokenizer tokens)
    {
        var token = tokens.Next();
        return Is(token, keyword) ? tokens.Next() : token;
    }

    // EXEC @ret = dbo.P: the variable receives the return value, the procedure follows the =.
    private static ReadOnlySpan<char> Procedure(ref SQLTokenizer tokens)
    {
        var name = tokens.Next();
        var rest = tokens;
        if (name.StartsWith("@") && rest.Next() is "=")
        {
            tokens = rest;
            return tokens.Next();
        }

        return name;
    }

    private static string Compose(ReadOnlySpan<char> operation, ReadOnlySpan<char> target)
    {
        if (operation.IsEmpty || operation.Length > MaxOperationLength || operation.ContainsAnyExcept(AsciiLetters))
        {
            return "SQL";
        }

        Span<char> name = stackalloc char[4 + MaxOperationLength + 1 + MaxTargetLength];
        "SQL ".CopyTo(name);
        var length = 4 + operation.ToUpperInvariant(name[4..]);
        length += WriteTarget(target, name[length..]);
        return new string(name[..length]);
    }

    // Writes " schema.table" and returns its length, or 0 for no usable target (a variable, a literal, too long).
    private static int WriteTarget(ReadOnlySpan<char> target, Span<char> destination)
    {
        if (!SQLTokenizer.IsName(target) || target[0] == '@')
        {
            return 0;
        }

        SQLTokenizer.LastTwoParts(target, out var schema, out var table);
        var length = schema.IsEmpty ? table.Length : schema.Length + 1 + table.Length;
        if (table.IsEmpty || length > MaxTargetLength)
        {
            return 0;
        }

        destination[0] = ' ';
        var written = 1;
        if (!schema.IsEmpty)
        {
            schema.CopyTo(destination[written..]);
            written += schema.Length;
            destination[written++] = '.';
        }

        table.CopyTo(destination[written..]);
        return written + table.Length;
    }

    private static bool Is(ReadOnlySpan<char> token, string keyword) => token.Equals(keyword, StringComparison.OrdinalIgnoreCase);
}
