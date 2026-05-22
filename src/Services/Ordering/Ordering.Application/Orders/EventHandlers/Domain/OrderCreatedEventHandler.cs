using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Core.Events;

namespace Ordering.Application.Orders.EventHandlers.Domain
{
    public class OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger) : INotificationHandler<OrderCreatedEvent>
    {
        public Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("Handling OrderCreatedEvent for OrderId: {OrderId}", notification.Order.Id);
            return Task.CompletedTask;
        }
    }
}
