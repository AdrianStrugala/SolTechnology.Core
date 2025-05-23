namespace SolTechnology.Core.Journey.Workflow
{
    public enum FlowStatus
    {
        NotStarted,
        Running,
        WaitingForInput,
        Suspended,
        Completed,
        Failed,
        Cancelled
    }
}
