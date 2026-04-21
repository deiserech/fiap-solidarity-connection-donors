namespace SolidarityConnection.Infrastructure.ServiceBus
{
    public interface IServiceBusPublisher
    {
        Task PublishAsync<T>(T @event, string topicName);
    }
}

