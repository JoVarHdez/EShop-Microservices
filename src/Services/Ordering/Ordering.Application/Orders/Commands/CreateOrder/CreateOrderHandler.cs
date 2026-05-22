using BuildingBlocks.CQRS;
using Ordering.Application.Data;
using Ordering.Application.DTOs;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;

namespace Ordering.Application.Orders.Commands.CreateOrder
{
    public class CreateOrderHandler(IApplicationDbContext dbContext) : ICommandHandler<CreateOrderCommand, CreateOrderResult>
    {
        public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            var order = CreateNewOrder(request.Order);

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new CreateOrderResult(order.Id.Value);
        }

        private static Order CreateNewOrder(OrderDto order)
        {
            var shippingAddress = Address.Of(order.ShippingAddress.FirstName, order.ShippingAddress.LastName, order.ShippingAddress.EmailAddress, order.ShippingAddress.AddressLine, order.ShippingAddress.Country, order.ShippingAddress.State, order.ShippingAddress.ZipCode);
            var billingAddress = Address.Of(order.BillingAddress.FirstName, order.BillingAddress.LastName, order.BillingAddress.EmailAddress, order.BillingAddress.AddressLine, order.BillingAddress.Country, order.BillingAddress.State, order.BillingAddress.ZipCode);
            var payment = Payment.Of(order.Payment.CardName, order.Payment.CardNumber, order.Payment.Expiration, order.Payment.Cvv, order.Payment.PaymentMethod);

            var newOrder = Order.Create(OrderId.Of(Guid.NewGuid()), CustomerId.Of(order.CustomerId), OrderName.Of(order.OrderName), shippingAddress, billingAddress, payment);

            foreach (var orderItem in order.OrderItems)
            {
                newOrder.Add(ProductId.Of(orderItem.ProductId), orderItem.Quantity, orderItem.Price);
            }

            return newOrder;
        }
    }
}
