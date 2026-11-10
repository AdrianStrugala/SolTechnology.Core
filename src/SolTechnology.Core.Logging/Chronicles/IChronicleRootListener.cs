namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>Receives every root after it closed; the reporter turns it into one log entry.</summary>
internal interface IChronicleRootListener
{
    void OnRootCompleted(ChronicleRoot root);
}
