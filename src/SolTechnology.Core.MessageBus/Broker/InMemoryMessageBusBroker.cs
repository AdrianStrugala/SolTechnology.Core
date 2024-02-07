using Azure.Messaging.ServiceBus;
using SolTechnology.Core.MessageBus.Publish;
using SolTechnology.Core.MessageBus.Receive;

namespace SolTechnology.Core.MessageBus.Broker
{
    public class InMemoryMessageBusBroker : IMessageBusBroker
    {
        private static readonly List<(string, InMemoryQueue)> MessageToQueueMap = new();

        public void RegisterTopicPublisher(string messageType, string topicName)
        {
            throw new NotImplementedException();
        }

        public List<ISender> ResolveMessagePublisher(string messageType)
        {
            var senders = MessageToQueueMap
                .Where(m => m.Item1.Equals(messageType, StringComparison.CurrentCultureIgnoreCase)).Select(x => x.Item2)
                .Cast<ISender>()
                .ToList();

            if (!senders.Any())
            {
                throw new ArgumentException($"No message bus Publisher for Message: [{messageType}] is configured.");
            }

            return senders;
        }

        public void RegisterTopicReceiver(Type messageType, string topicName, string subscriptionName)
        {
            throw new NotImplementedException();
        }

        public List<ServiceBusProcessor> ResolveMessageReceiver(string messageType)
        {
            throw new NotImplementedException();
        }

        public List<(Type, IReceiver)> ResolveMessageReceivers()
        {
            throw new NotImplementedException();
        }

        public void RegisterQueuePublisher(string messageType, string queueName)
        {
            var existingQueue = MessageToQueueMap.FirstOrDefault(x =>
                x.Item1 == messageType && x.Item2.QueueName == queueName);

            if (existingQueue.Item1 == null)
            {
                var queue = new InMemoryQueue(queueName);
                MessageToQueueMap.Add((messageType, queue));
            }
        }

        public void RegisterQueueReceiver(Type messageType, string queueName)
        {
            var existingQueue = MessageToQueueMap.FirstOrDefault(x =>
                x.Item1 == messageType.ToString() && x.Item2.QueueName == queueName);

            if (existingQueue.Item1 == null)
            {
                var queue = new InMemoryQueue(queueName);
                MessageToQueueMap.Add((messageType.ToString(), queue));
            }
        }
    }
}
