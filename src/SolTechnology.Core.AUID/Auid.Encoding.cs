using System.Runtime.CompilerServices;

namespace SolTechnology.Core.AUID;

public readonly partial struct Auid
{
    private static Auid CreateFromCode(string code, TimeProvider timeProvider)
    {
        ValidateCode(code.AsSpan(), nameof(code));
        return CreateInternal(code.AsSpan(), generateCode: false, timeProvider);
    }

    private static void ValidateCode(ReadOnlySpan<char> code, string? paramName)
    {
        if (code.Length != 3)
        {
            throw new ArgumentException("Code must be exactly 3 characters.", paramName);
        }

        for (int i = 0; i < 3; i++)
        {
            if (code[i] < 'A' || code[i] > 'Z')
            {
                throw new ArgumentException("Code must contain only uppercase letters A-Z.", paramName);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Auid CreateInternal(ReadOnlySpan<char> input, bool generateCode, TimeProvider timeProvider)
    {
        long codeEncoded = generateCode ? EncodeGeneratedCode(input) : Encode3Chars(input);
        long seconds = (timeProvider.GetUtcNow().UtcTicks - EpochTicks) / TimeSpan.TicksPerSecond;

        if (seconds < 0)
        {
            throw new InvalidOperationException(
                "AUID timestamp underflow: the clock is before the AUID epoch 2001-01-01T00:00:00Z.");
        }

        if (seconds > MaskTime)
        {
            throw new InvalidOperationException(
                "AUID timestamp overflow: Przekroczono rok 2137! Papież Polak robi BUM i AUID już nie działa. Czas na nową epokę, albo niech ktoś w końcu przepisze ten system na 64-bitowy timestamp!");
        }

        long randomEncoded = Random.Shared.Next() & MaskRandom;
        return new Auid(Compose(codeEncoded, seconds & MaskTime, randomEncoded));
    }

    private static long Compose(long codeEncoded, long timeEncoded, long randomEncoded)
    {
        return (codeEncoded << (BitsTime + BitsRandom))
             | (timeEncoded << BitsRandom)
             | randomEncoded;
    }

    private static ReadOnlySpan<char> FileNameWithoutExtension(ReadOnlySpan<char> path)
    {
        int lastSeparator = path.LastIndexOfAny('/', '\\');
        int lastDot = path.LastIndexOf('.');

        int start = lastSeparator + 1;
        int length = (lastDot > start) ? (lastDot - start) : (path.Length - start);
        return path.Slice(start, length);
    }

    private static long EncodeGeneratedCode(ReadOnlySpan<char> name)
    {
        Span<char> codeBuffer = stackalloc char[3];
        GenerateCode3(name, codeBuffer);
        return Encode3Chars(codeBuffer);
    }

    /// <summary>
    /// Generates "ORD" from "Order", "USR" from "User" without allocations.
    /// </summary>
    private static void GenerateCode3(ReadOnlySpan<char> input, Span<char> output)
    {
        if (input.IsEmpty)
        {
            DefaultCode.AsSpan().CopyTo(output);
            return;
        }

        int outIdx = 0;
        char first = ToUpperASCII(input[0]);
        output[outIdx++] = IsLetterAToZ(first) ? first : 'X';

        for (int i = 1; i < input.Length && outIdx < 3; i++)
        {
            char c = ToUpperASCII(input[i]);
            if (IsLetterAToZ(c) && !IsVowel(c))
            {
                output[outIdx++] = c;
            }
        }

        while (outIdx < 3)
        {
            output[outIdx++] = 'X';
        }
    }

    private static char ToUpperASCII(char c) => c >= 'a' && c <= 'z' ? (char)(c - 32) : c;

    private static bool IsLetterAToZ(char c) => c >= 'A' && c <= 'Z';

    private static bool IsVowel(char c) => c == 'A' || c == 'E' || c == 'I' || c == 'O' || c == 'U' || c == 'Y';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Encode3Chars(ReadOnlySpan<char> code)
    {
        // A=0, B=1...
        return ((long)(code[0] - 'A') * 676) +
               ((long)(code[1] - 'A') * 26) +
               ((long)(code[2] - 'A'));
    }
}
