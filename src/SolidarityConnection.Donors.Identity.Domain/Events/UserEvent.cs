namespace SolidarityConnection.Donors.Identity.Domain.Events
{
    public record UserEvent(
    Guid DonorId,
    string Email,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? RemovedAt);
}

