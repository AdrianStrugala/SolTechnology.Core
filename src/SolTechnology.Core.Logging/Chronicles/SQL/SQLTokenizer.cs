namespace SolTechnology.Core.Logging.Chronicles.SQL;

/// <summary>
/// Splits T-SQL text into names (multi-part, possibly quoted with <c>[]</c>, <c>""</c> or backticks),
/// string literals and single symbols, skipping whitespace and comments. It only finds keywords
/// outside literals and quoted names for <see cref="SQLStepNamer"/>; it is not a parser.
/// </summary>
internal ref struct SQLTokenizer(ReadOnlySpan<char> text)
{
    private readonly ReadOnlySpan<char> _text = text;
    private int _position;

    /// <summary>The next token, or an empty span at the end of the text.</summary>
    internal ReadOnlySpan<char> Next()
    {
        SkipTrivia();
        if (_position >= _text.Length)
        {
            return [];
        }

        var start = _position;
        if (_text[_position] == '\'')
        {
            SkipQuoted('\'');
        }
        else if (IsNameStart(_text[_position]))
        {
            ReadName();
        }
        else
        {
            _position++;
        }

        return _text[start.._position];
    }

    /// <summary>Moves past the next <c>;</c>; returns <see langword="false"/> when there is none.</summary>
    internal bool SkipPastSemicolon()
    {
        for (var token = Next(); !token.IsEmpty; token = Next())
        {
            if (token is ";")
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsName(ReadOnlySpan<char> token) => !token.IsEmpty && IsNameStart(token[0]);

    /// <summary>The last two parts of a name token without their quotes: <c>[db].[dbo].[T]</c> gives <c>dbo</c> and <c>T</c>.</summary>
    internal static void LastTwoParts(ReadOnlySpan<char> name, out ReadOnlySpan<char> schema, out ReadOnlySpan<char> table)
    {
        var parts = new SQLTokenizer(name);
        schema = table = [];
        while (true)
        {
            var start = parts._position;
            parts.ReadPart();
            schema = table;
            table = Unquote(name[start..parts._position]);
            if (parts._position >= name.Length || name[parts._position] != '.')
            {
                return;
            }

            parts._position++;
        }
    }

    private void ReadName()
    {
        ReadPart();
        while (_position < _text.Length && _text[_position] == '.')
        {
            _position++;
            ReadPart();
        }
    }

    private void ReadPart()
    {
        if (_position < _text.Length && _text[_position] is '[' or '"' or '`')
        {
            SkipQuoted(_text[_position] == '[' ? ']' : _text[_position]);
            return;
        }

        while (_position < _text.Length && IsWordChar(_text[_position]))
        {
            _position++;
        }
    }

    // A doubled closing quote ('' or ]]) is an escaped character, so the scan goes on past it.
    private void SkipQuoted(char close)
    {
        do
        {
            var at = _text[(_position + 1)..].IndexOf(close);
            _position = at < 0 ? _text.Length : _position + at + 2;
        }
        while (_position < _text.Length && _text[_position] == close);
    }

    private void SkipTrivia()
    {
        while (_position < _text.Length)
        {
            var rest = _text[_position..];
            if (char.IsWhiteSpace(rest[0]))
            {
                _position++;
            }
            else if (rest.StartsWith("--") || rest.StartsWith("/*"))
            {
                var terminator = rest[0] == '-' ? "\n" : "*/";
                _position = rest.IndexOf(terminator) is var at and >= 0 ? _position + at + terminator.Length : _text.Length;
            }
            else
            {
                return;
            }
        }
    }

    private static ReadOnlySpan<char> Unquote(ReadOnlySpan<char> part) => part switch
    {
        ['[', .. var inner, ']'] => inner,
        ['"', .. var inner, '"'] => inner,
        ['`', .. var inner, '`'] => inner,
        _ => part
    };

    private static bool IsNameStart(char c) => IsWordChar(c) || c is '[' or '"' or '`';

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';
}
