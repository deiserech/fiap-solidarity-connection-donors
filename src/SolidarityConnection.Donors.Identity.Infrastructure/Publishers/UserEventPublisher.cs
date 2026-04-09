using SolidarityConnection.Donors.Identity.Application.Interfaces.Publishers;
using SolidarityConnection.Donors.Identity.Domain.Entities;
using SolidarityConnection.Donors.Identity.Domain.Events;
using SolidarityConnection.Donors.Identity.Infrastructure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace SolidarityConnection.Donors.Identity.Infrastructure.Publishers
{
    public class UserEventPublisher : IUserEventPublisher
    {
        private readonly IServiceBusPublisher _busPublisher;
        private readonly ILogger<UserEventPublisher> _logger;
        private const string UserTopic = "donors-upserted";

        public UserEventPublisher(IServiceBusPublisher busPublisher, ILogger<UserEventPublisher> logger)
        {
            _busPublisher = busPublisher;
            _logger = logger;
        }

        public async Task PublishUserEventAsync(User user, bool isRemoved = false)
        {
            DateTimeOffset? removedAt = isRemoved ? DateTimeOffset.UtcNow : null;
            var evt = new UserEvent(user.Id, user.Email, DateTimeOffset.Now, removedAt);
            try
            {
                await _busPublisher.PublishAsync(evt, UserTopic);
            }
            catch (Exception e)
            {
                _logger.LogError("Erro ao publicar evento {Evento}: {DonorId}. Message: {Message}", nameof(UserEvent), user.Id, e.Message);
            }
        }
    }
}

