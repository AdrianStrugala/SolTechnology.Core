using System.Runtime.CompilerServices;

namespace SolTechnology.Core.AUID;

public readonly partial struct Auid
{
    /// <summary>
    /// Creates a new AUID based on the type name (e.g., Order -> ORD).
    /// Zero allocation.
    /// </summary>
    /// <typeparam name="T">The type to derive the 3-letter code from.</typeparam>
    /// <returns>A new AUID with code derived from type name.</returns>
    /// <example>
    /// <code>
    /// var orderId = Auid.New&lt;Order&gt;();
    /// Console.WriteLine(orderId); // Output: ORD_20241205123456_012345
    /// </code>
    /// </example>
    /// <exception cref="InvalidOperationException">Thrown when current date exceeds year 2137 (timestamp overflow).</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Auid New<T>()
    {
        return CreateInternal(typeof(T).Name.AsSpan(), generateCode: true, TimeProvider.System);
    }

    /// <summary>
    /// Creates a new AUID based on the type name (e.g., Order -> ORD), with the timestamp taken from
    /// <paramref name="timeProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type to derive the 3-letter code from.</typeparam>
    /// <param name="timeProvider">The clock that supplies the timestamp, e.g. a FakeTimeProvider in tests.</param>
    /// <returns>A new AUID with code derived from type name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeProvider"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the time exceeds year 2137 (timestamp overflow).</exception>
    public static Auid New<T>(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return CreateInternal(typeof(T).Name.AsSpan(), generateCode: true, timeProvider);
    }

    /// <summary>
    /// Creates a new AUID.
    /// If code is provided, uses that 3-letter code (must be exactly 3 uppercase letters A-Z).
    /// If code is null, infers the code from the source file name using [CallerFilePath].
    /// </summary>
    /// <param name="code">Optional 3-letter code (must be uppercase A-Z). If null, uses caller file name.</param>
    /// <param name="callerFilePath">Automatically populated by compiler. Do not pass manually.</param>
    /// <returns>A new AUID with specified or inferred code.</returns>
    /// <example>
    /// <code>
    /// // Explicit code
    /// var userId = Auid.New("USR");
    ///
    /// // Inferred from file name (e.g., OrderService.cs -> ORD)
    /// var id = Auid.New();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown when code is not exactly 3 characters or contains non-uppercase letters.</exception>
    /// <exception cref="InvalidOperationException">Thrown when current date exceeds year 2137 (timestamp overflow).</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Auid New(string? code = null, [CallerFilePath] string callerFilePath = "")
    {
        if (code != null)
        {
            return CreateFromCode(code, TimeProvider.System);
        }

        ReadOnlySpan<char> path = callerFilePath.AsSpan();
        if (path.IsEmpty)
        {
            return CreateInternal(DefaultCode.AsSpan(), generateCode: false, TimeProvider.System);
        }

        return CreateInternal(FileNameWithoutExtension(path), generateCode: true, TimeProvider.System);
    }

    /// <summary>
    /// Creates a new AUID with the timestamp taken from <paramref name="timeProvider"/>.
    /// If code is provided, uses that 3-letter code (must be exactly 3 uppercase letters A-Z); if code is null, uses "XXX".
    /// </summary>
    /// <param name="code">Optional 3-letter code (must be uppercase A-Z). If null, "XXX" is used.</param>
    /// <param name="timeProvider">The clock that supplies the timestamp, e.g. a FakeTimeProvider in tests.</param>
    /// <returns>A new AUID with the specified code.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeProvider"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when code is not exactly 3 characters or contains non-uppercase letters.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the time exceeds year 2137 (timestamp overflow).</exception>
    public static Auid New(string? code, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return code == null
            ? CreateInternal(DefaultCode.AsSpan(), generateCode: false, timeProvider)
            : CreateFromCode(code, timeProvider);
    }

    /// <summary>
    /// Creates a new AUID from a specific 3-letter code (must be exactly 3 uppercase letters A-Z).
    /// Zero allocation version using ReadOnlySpan.
    /// </summary>
    /// <param name="code">3-letter code span (must be uppercase A-Z).</param>
    /// <returns>A new AUID with the specified code.</returns>
    /// <example>
    /// <code>
    /// ReadOnlySpan&lt;char&gt; code = "PRD".AsSpan();
    /// var productId = Auid.New(code);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">Thrown when code is not exactly 3 characters or contains non-uppercase letters.</exception>
    /// <exception cref="InvalidOperationException">Thrown when current date exceeds year 2137 (timestamp overflow).</exception>
    public static Auid New(ReadOnlySpan<char> code)
    {
        ValidateCode(code, paramName: null);
        return CreateInternal(code, generateCode: false, TimeProvider.System);
    }
}
