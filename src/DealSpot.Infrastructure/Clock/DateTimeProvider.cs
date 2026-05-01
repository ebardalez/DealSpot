using DealSpot.Application.Abstractions.Clock;

namespace DealSpot.Infrastructure.Clock;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
