using Ordering.Core.Abstractions;
using Ordering.Core.Models;

namespace Ordering.Core.Events
{
    public record OrderUpdatedEvent(Order Order) : IDomainEvent;
}
