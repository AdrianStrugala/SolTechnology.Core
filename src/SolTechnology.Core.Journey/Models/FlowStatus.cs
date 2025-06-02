namespace SolTechnology.Core.Journey.Models
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
