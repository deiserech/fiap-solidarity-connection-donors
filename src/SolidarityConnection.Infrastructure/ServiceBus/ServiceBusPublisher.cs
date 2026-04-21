using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace SolidarityConnection.Infrastructure.ServiceBus
{
    public class ServiceBusPublisher : IServiceBusPublisher
    {
        private readonly ServiceBusClient _client;

        public ServiceBusPublisher(ServiceBusClient client)
        {
            _client = client;
        }

        public async Task PublishAsync<T>(T @event, string topicName)
        {
            var messageBody = JsonSerializer.Serialize(@event);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody))
            {
                ContentType = "application/json"
            };

            var sender = _client.CreateSender(topicName);
            await sender.SendMessageAsync(message);
        }
    }
}


