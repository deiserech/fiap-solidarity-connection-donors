using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace SolidarityConnection.Infrastructure.ServiceBus
{
    public class ServiceBusClientWrapper : IServiceBusClientWrapper
    {
        private readonly ServiceBusClient _client;

        public ServiceBusClientWrapper(ServiceBusClient client)
        {
            _client = client;
        }

        public IServiceBusProcessor CreateProcessorWrapper(string topicName, string subscriptionName, ServiceBusProcessorOptions? options = null)
        {
            var processor = _client.CreateProcessor(topicName, subscriptionName, options ?? new ServiceBusProcessorOptions { MaxConcurrentCalls = 1, AutoCompleteMessages = false });
            return new ServiceBusProcessorWrapper(processor);
        }
    }
}

