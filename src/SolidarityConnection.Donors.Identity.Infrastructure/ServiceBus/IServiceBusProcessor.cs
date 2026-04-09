using Azure.Messaging.ServiceBus;

namespace SolidarityConnection.Donors.Identity.Infrastructure.ServiceBus
{
    public interface IServiceBusProcessor
    {
        event Func<ProcessMessageEventArgs, Task> ProcessMessageAsync;
        event Func<ProcessErrorEventArgs, Task> ProcessErrorAsync;
        Task StartProcessingAsync(CancellationToken cancellationToken = default);
    }
}

