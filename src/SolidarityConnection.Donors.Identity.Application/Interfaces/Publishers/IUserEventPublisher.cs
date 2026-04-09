using SolidarityConnection.Donors.Identity.Domain.Entities;

namespace SolidarityConnection.Donors.Identity.Application.Interfaces.Publishers;
public interface IUserEventPublisher
{
    Task PublishUserEventAsync(User user, bool isRemoved = false);
}

