
namespace SolTechnology.Core.MessageBus
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