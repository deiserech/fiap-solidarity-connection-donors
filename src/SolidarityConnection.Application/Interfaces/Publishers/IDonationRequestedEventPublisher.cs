using SolidarityConnection.Domain.Events;

namespace SolidarityConnection.Application.Interfaces.Publishers
{
    public interface IDonationRequestedEventPublisher
    {
        Task PublishAsync(DonationRequestedEvent @event);
    }
}