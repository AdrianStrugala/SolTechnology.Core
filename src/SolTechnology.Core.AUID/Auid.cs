using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

public readonly struct Auid : IComparable<Auid>, IEquatable<Auid>
{
    /*
     * Struktura 64-bitowego AUID:
     *
     * Bity 63-49 (15 bitów): 3-znakowy kod (AAA-ZZZ) = 17,576 kombinacji
     * Bity 48-17 (32 bity):  Timestamp w sekundach od 2001-01-01 (do 2137)
     * Bity 16-0  (17 bitów): Losowa liczba (0-131,071)
     */
    
    // Fizyczna wartość (8 bajtów)
    public long Value { get; }


    // Epoch: 1 Stycznia 2020
    private static readonly DateTime Epoch = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Random Rng = Random.Shared;
    private static readonly string DefaultCodeIfEmpty = "XXX";

    // === KONFIGURACJA (63 bity) ===
    private const int BitsCode = 15;   // 3 litery
    private const int BitsTime = 32;   // ~136 lat
    private const int BitsRandom = 16; // 65k ID/sek

    
    // Prywatny konstruktor - jedyny sposób to użycie metod New()
    private Auid(long value) => Value = value;

    

    /// <summary>
    /// Tworzy AUID automatycznie wykrywając nazwę klasy wywołującej.
    /// UWAGA: Wolniejsze przez StackFrame. Zaleca się używanie New<T>().
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Auid New()
    {
        var frame = new StackFrame(1, false);
        var type = frame.GetMethod()?.DeclaringType;
        string name = type != null ? type.Name : DefaultCodeIfEmpty;
        
        // Tutaj generujemy kod z nazwy (więc nie rzuci wyjątku o długość)
        return CreateInternal(GenerateCodeFromName(name));
    }

    /// <summary>
    /// Tworzy AUID dla konkretnego typu (Najszybsza metoda).
    /// Np. Auid.New<Order>() wygeneruje kod "ORD"
    /// </summary>
    public static Auid New<T>()
    {
        string name = typeof(T).Name;
        return CreateInternal(GenerateCodeFromName(name));
    }

    /// <summary>
    /// Tworzy AUID ręcznie podając kod 3-literowy.
    /// Rzuca wyjątek, jeśli kod nie ma dokładnie 3 znaków.
    /// </summary>
    public static Auid New(string code)
    {
        if (code == null || code.Length != 3)
            throw new ArgumentException("Kod identyfikatora musi mieć dokładnie 3 znaki.", nameof(code));

        // Sprawdzamy czy to same litery (opcjonalnie)
        if (!code.All(char.IsLetter))
             throw new ArgumentException("Kod może zawierać tylko litery.", nameof(code));

        return CreateInternal(code);
    }

    // Wewnętrzna metoda tworząca strukturę
    private static Auid CreateInternal(string code3Chars)
    {
        long codeVal = EncodeCode(code3Chars);
        long timeVal = (long)(DateTime.UtcNow - Epoch).TotalSeconds;

        // Zabezpieczenie zakresu czasu (32 bity)
        long maxTime = (1L << BitsTime) - 1;
        if (timeVal > maxTime) timeVal = maxTime;

        int randomVal = Rng.Next(0, 1 << BitsRandom);

        // Bit shifting: [KOD] [CZAS] [LOS]
        long finalId = (codeVal << (BitsTime + BitsRandom))
                     | (timeVal << BitsRandom)
                     | (uint)randomVal;

        return new Auid(finalId);
    }

    /// <summary>
    /// Twój algorytm: 1 litera startowa + reszta bez samogłosek + padding X.
    /// Zawsze zwraca 3 znaki.
    /// </summary>
    private static string GenerateCodeFromName(string name)
    {
        if (string.IsNullOrEmpty(name)) return DefaultCodeIfEmpty;
        
        name = name.ToUpper();
        char[] vowels = { 'A', 'E', 'I', 'O', 'U', 'Y' };
        var sb = new StringBuilder();

        // 1. Zawsze bierzemy pierwszą literę
        sb.Append(name[0]);

        // 2. Iterujemy od drugiej litery i pomijamy samogłoski
        for (int i = 1; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsLetter(c) && !vowels.Contains(c))
            {
                sb.Append(c);
            }
            // Jeśli już mamy 3 znaki, przerywamy
            if (sb.Length >= 3) break;
        }

        // 3. Dopełnianie 'X'
        while (sb.Length < 3)
        {
            sb.Append('X');
        }

        return sb.ToString();
    }

    private static long EncodeCode(string code)
    {
        code = code.ToUpper();
        return ((code[0] - 'A') * 676) +
               ((code[1] - 'A') * 26) +
               (code[2] - 'A');
    }

    private static string DecodeCode(long value)
    {
        if (value == 0) return DefaultCodeIfEmpty; // Obsługa Empty

        long codeVal = value >> (BitsTime + BitsRandom);
        
        char c1 = (char)('A' + (codeVal / 676));
        char c2 = (char)('A' + ((codeVal % 676) / 26));
        char c3 = (char)('A' + (codeVal % 26));
        
        return $"{c1}{c2}{c3}";
    }
    
    // Wartość "Pusta" (same zera)
    public static readonly Auid Empty = new Auid(0);
    
    public override string ToString()
    {
        if (Value == 0) return $"{DefaultCodeIfEmpty}_00000000_0000";

        string code = DecodeCode(Value);
        
        long timeMask = (1L << BitsTime) - 1;
        long timeVal = (Value >> BitsRandom) & timeMask;
        DateTime date = Epoch.AddSeconds(timeVal);

        long randomMask = (1L << BitsRandom) - 1;
        long randomVal = Value & randomMask;

        // Format: KOD_DATA_HEX
        return $"{code}_{date:yyyyMMdd}_{randomVal:X4}";
    }

    // Konwersje
    public static implicit operator long(Auid auid) => auid.Value;
    public static implicit operator Auid(long value) => new Auid(value);

    // Porównania
    public int CompareTo(Auid other) => Value.CompareTo(other.Value);
    public bool Equals(Auid other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is Auid other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(Auid left, Auid right) => left.Equals(right);
    public static bool operator !=(Auid left, Auid right) => !left.Equals(right);
}