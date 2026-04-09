using Azure.Messaging.ServiceBus;

namespace SolidarityConnection.Donors.Identity.Infrastructure.ServiceBus
{
    public interface IServiceBusClientWrapper
    {
        ServiceBusSender GetSender(string queueName);
        ServiceBusProcessor CreateProcessor(string queueName, ServiceBusProcessorOptions? options = null);
        ServiceBusProcessor CreateProcessor(string topicName, string subscriptionName, ServiceBusProcessorOptions? options = null);
        IServiceBusProcessor CreateProcessorWrapper(string topicName, string subscriptionName, ServiceBusProcessorOptions? options = null);
    }
}

