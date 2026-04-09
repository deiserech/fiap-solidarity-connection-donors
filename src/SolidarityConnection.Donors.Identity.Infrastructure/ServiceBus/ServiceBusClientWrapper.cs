using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace SolidarityConnection.Donors.Identity.Infrastructure.ServiceBus
{
    public class ServiceBusClientWrapper : IServiceBusClientWrapper
    {
        private readonly ServiceBusClient _client;

        public ServiceBusClientWrapper(IOptions<ServiceBusOptions> options)
        {
            _client = new ServiceBusClient(options.Value.ConnectionString);
        }

        public ServiceBusSender GetSender(string queueName) => _client.CreateSender(queueName);

        public ServiceBusProcessor CreateProcessor(string queueName, ServiceBusProcessorOptions? options = null)
            => _client.CreateProcessor(queueName, options ?? new ServiceBusProcessorOptions { MaxConcurrentCalls = 1, AutoCompleteMessages = false });

        public ServiceBusProcessor CreateProcessor(string topicName, string subscriptionName, ServiceBusProcessorOptions? options = null)
            => _client.CreateProcessor(topicName, subscriptionName, options ?? new ServiceBusProcessorOptions { MaxConcurrentCalls = 1, AutoCompleteMessages = false });

        public IServiceBusProcessor CreateProcessorWrapper(string topicName, string subscriptionName, ServiceBusProcessorOptions? options = null)
        {
            var processor = _client.CreateProcessor(topicName, subscriptionName, options ?? new ServiceBusProcessorOptions { MaxConcurrentCalls = 1, AutoCompleteMessages = false });
            return new ServiceBusProcessorWrapper(processor);
        }
    }
}

