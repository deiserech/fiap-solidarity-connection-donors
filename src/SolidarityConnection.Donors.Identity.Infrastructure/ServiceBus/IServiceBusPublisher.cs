namespace SolidarityConnection.Donors.Identity.Infrastructure.ServiceBus
{
    public interface IServiceBusPublisher
    {
        Task PublishAsync<T>(T @event, string topicName);
    }
}

