namespace SolTechnology.Core.Flow.Models
{
    public enum FlowStatus
    {
        Created,
        Running,
        WaitingForInput,
        Completed,
        Failed,
        Cancelled
    }
}
