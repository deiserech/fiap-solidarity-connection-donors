using Azure.Messaging.ServiceBus;

namespace SolidarityConnection.Infrastructure.ServiceBus
{
    internal class ServiceBusProcessorWrapper : IServiceBusProcessor
    {
        private readonly ServiceBusProcessor _processor;

        public ServiceBusProcessorWrapper(ServiceBusProcessor processor)
        {
            _processor = processor;
        }

        public event Func<ProcessMessageEventArgs, Task> ProcessMessageAsync
        {
            add => _processor.ProcessMessageAsync += value;
            remove => _processor.ProcessMessageAsync -= value;
        }

        public event Func<ProcessErrorEventArgs, Task> ProcessErrorAsync
        {
            add => _processor.ProcessErrorAsync += value;
            remove => _processor.ProcessErrorAsync -= value;
        }

        public Task StartProcessingAsync(CancellationToken cancellationToken = default)
            => _processor.StartProcessingAsync(cancellationToken);
    }
}

