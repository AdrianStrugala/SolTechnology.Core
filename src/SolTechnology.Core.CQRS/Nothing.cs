namespace SolTechnology.Core.CQRS;

/// <summary>
/// Unit type representing the absence of a meaningful value.
/// Used as the result type for <see cref="ICommand"/> (side-effect-only commands).
/// </summary>
public readonly struct Nothing
{
    //I'm just void. Or nothing
}
