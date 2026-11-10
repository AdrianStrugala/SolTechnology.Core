namespace SolTechnology.Core.MessageBus.Receive;

/// <summary>How the receiver settled a message.</summary>
internal enum MessageSettlementAction
{
    Completed,
    Abandoned,
    DeadLettered
}

/// <summary>
/// The settlement <see cref="MessageSettler"/> chose for a message, and whether the broker accepted it.
/// Only the dead-letter reason code is kept, never the description, which can carry exception text.
/// </summary>
internal readonly record struct MessageSettlement(MessageSettlementAction Action, string? DeadLetterReason, bool Settled);
