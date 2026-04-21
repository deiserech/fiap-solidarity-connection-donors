using SolidarityConnection.Domain.Entities;

namespace SolidarityConnection.Application.Interfaces.Publishers;
public interface IUserEventPublisher
{
    Task PublishUserEventAsync(User user, bool isRemoved = false);
}

