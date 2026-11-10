namespace SolTechnology.Core.Logging.Chronicles;

/// <summary>Outcome of a Chronicle step or root.</summary>
public enum ChronicleStepStatus
{
    /// <summary>The step finished without <see cref="ChronicleStep.Fail(Exception)"/> or <see cref="ChronicleStep.Cancel"/>.</summary>
    Ok,

    /// <summary>The step was marked failed through <see cref="ChronicleStep.Fail(Exception)"/>.</summary>
    Failed,

    /// <summary>The step was marked cancelled through <see cref="ChronicleStep.Cancel"/>.</summary>
    Cancelled
}
