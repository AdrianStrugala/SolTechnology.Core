namespace SolTechnology.Core.Journey.Models
{
    public enum FlowStatus
    {
        NotStarted,
        Running,
        WaitingForInput,
        Completed,
        Failed,
        Cancelled
    }
}
