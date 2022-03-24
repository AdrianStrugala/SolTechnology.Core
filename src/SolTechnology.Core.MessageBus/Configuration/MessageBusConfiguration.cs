
namespace SolTechnology.Core.MessageBus.Configuration
{
    public class MessageBusConfiguration
    {
        public string ConnectionString { get; set; }
        public List<Publisher> Publishers { get; set; } = new();
    }

    public class Publisher
    {
        public string Topic { get; set; }
    }
}