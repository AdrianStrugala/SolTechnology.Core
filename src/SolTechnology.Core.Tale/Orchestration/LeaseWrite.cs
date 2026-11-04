namespace SolTechnology.Core.Tale.Orchestration;

/// <summary>The Running-lease saves that <see cref="LeaseReadBackCheck"/> verifies once per host.</summary>
internal enum LeaseWrite
{
    /// <summary>The first save of a new tale.</summary>
    ClaimByInsert,

    /// <summary>The first save of a resumed run, which replaces a stored row.</summary>
    ClaimByUpdate,

    /// <summary>A heartbeat renewal of the lease while a chapter executes.</summary>
    Renewal
}
