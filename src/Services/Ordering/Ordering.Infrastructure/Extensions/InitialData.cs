using Ordering.Core.Models;
using Ordering.Core.ValueObjects;

namespace Ordering.Infrastructure.Extensions
{
    public static class InitialData
    {
        public static readonly IEnumerable<Customer> Customers =
        [
            Customer.Create(CustomerId.Of(new Guid("00000000-0000-0000-0000-000000000001")), "John Doe", "johndoe@example.com"),
            Customer.Create(CustomerId.Of(new Guid("00000000-0000-0000-0000-000000000002")), "Jane Smith", "janesmith@example.com"),
        ];

        public static readonly IEnumerable<Product> Products =
        [
            Product.Create(ProductId.Of(new Guid("5334c996-8457-4cf0-815c-ed2b77c4ff61")), "IPhone X", 500),
            Product.Create(ProductId.Of(new Guid("c67d6323-e8b1-4bdf-9a75-b0d0d2e7e914")), "Samsung 10", 500),
            Product.Create(ProductId.Of(new Guid("00000000-0000-0000-0000-000000000013")), "Headphones", 199.99m),
            Product.Create(ProductId.Of(new Guid("00000000-0000-0000-0000-000000000014")), "Smartwatch", 299.99m),
        ];

        public static IEnumerable<Order> OrderWithItems
        {
            get
            {
                var order1 = Order.Create(OrderId.Of(Guid.NewGuid()),
                         CustomerId.Of(new Guid("00000000-0000-0000-0000-000000000001")),
                         OrderName.Of("ORD_1"),
                         Addresses.First(),
                         Addresses.First(),
                         Payments.First());
                order1.Add(ProductId.Of(new Guid("5334c996-8457-4cf0-815c-ed2b77c4ff61")), 1, 500);
                order1.Add(ProductId.Of(new Guid("00000000-0000-0000-0000-000000000013")), 2, 199.99m);

                var order2 = Order.Create(OrderId.Of(Guid.NewGuid()),
                         CustomerId.Of(new Guid("00000000-0000-0000-0000-000000000002")),
                         OrderName.Of("ORD_2"),
                         Addresses.Last(),
                         Addresses.Last(),
                         Payments.Last());

                order2.Add(ProductId.Of(new Guid("c67d6323-e8b1-4bdf-9a75-b0d0d2e7e914")), 1, 500);
                order2.Add(ProductId.Of(new Guid("00000000-0000-0000-0000-000000000014")), 2, 299.99m);

                return [order1, order2];
            }
        }

        private static readonly IEnumerable<Address> Addresses =
        [
            Address.Of("john", "doe", "john.doe@example.com", "some street address", "USA", "Florida", "35698"),
            Address.Of("jane", "smith", "jane.smith@example.com", "another street address", "USA", "California", "67890"),
        ];

        private static readonly IEnumerable<Payment> Payments =
        [
            Payment.Of("john doe", "4567456745684563", "01/12", "456", 1),
            Payment.Of("jane smith", "1234123412341234", "12/24", "123", 2),
        ];
    }
}