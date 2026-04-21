using Azure.Messaging.ServiceBus;

namespace SolidarityConnection.Infrastructure.ServiceBus
{
    public interface IServiceBusClientWrapper
    {
        IServiceBusProcessor CreateProcessorWrapper(string topicName, string subscriptionName, ServiceBusProcessorOptions? options = null);
    }
}

