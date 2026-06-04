using BuildingBlocks.Messaging.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Ordering.Application.DTOs;
using Ordering.Application.Orders.Commands.CreateOrder;
using Ordering.Core.Enums;
using Wolverine;

namespace Ordering.Application.Orders.EventHandlers.Integration
{
    public class BasketCheckoutEventHandler(IMessageBus bus, ILogger<BasketCheckoutEventHandler> logger)
        : IConsumer<BasketCheckoutEvent>
    {
        public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
        {
            logger.LogInformation("Integration Event: {EventName} - {Id}", context.Message.GetType().Name, context.Message.Id);

            var command = MapToCreateOrderCommand(context.Message);
            await bus.InvokeAsync(command);
        }

        private static CreateOrderCommand MapToCreateOrderCommand(BasketCheckoutEvent message)
        {
            var addressDto = new AddressDto(message.FirstName, message.LastName, message.EmailAddress, message.AddressLine, message.Country, message.State, message.ZipCode);
            var paymentDto = new PaymentDto(message.CardName, message.CardNumber, message.Expiration, message.CVV, message.PaymentMethod);
            var orderId = Guid.NewGuid();

            var orderDto = new OrderDto(
                Id: orderId,
                CustomerId: message.CustomerId,
                OrderName: message.UserName,
                ShippingAddress: addressDto,
                BillingAddress: addressDto,
                Payment: paymentDto,
                Status: OrderStatus.Pending,
                OrderItems: [
                    // In a real application, you would likely retrieve product details from a database or another service
                    new OrderItemDto(orderId, new Guid("00000000-0000-0000-0000-000000000001"), 2, 500),
                    new OrderItemDto(orderId, new Guid("00000000-0000-0000-0000-000000000002"), 1, 400)
                ]);

            return new CreateOrderCommand(orderDto);
        }
    }
}
