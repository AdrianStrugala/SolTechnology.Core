
namespace SolTechnology.Core.MessageBus.Configuration
{
    public class MessageBusConfiguration
    {
        public string ConnectionString { get; set; }
        public bool CreateResources { get; set; } = true;

        public List<QueueConfiguration> Queues { get; set; } = new List<QueueConfiguration>();
    }
}