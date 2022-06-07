
namespace SolTechnology.Core.MessageBus.Configuration
{
    public class MessageBusConfiguration
    {
        public string ConnectionString { get; set; }

        public List<QueueConfiguration> Queues { get; set; } = new List<QueueConfiguration>();
    }
}