using Microsoft.Extensions.Logging;
using SolidarityConnection.Application.Interfaces.Publishers;
using SolidarityConnection.Domain.Entities;
using SolidarityConnection.Domain.Events;
using SolidarityConnection.Infrastructure.ServiceBus;

namespace SolidarityConnection.Application.Publishers
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
            var evt = new UserEvent(user.Id, user.Email, DateTimeOffset.UtcNow, removedAt);
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

