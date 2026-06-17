using Shopping.Web.Razor.Models;
using Shopping.Web.Razor.Models.Basket;
using Shopping.Web.Razor.Models.Catalog;
using Shopping.Web.Razor.Models.Ordering;

namespace Shopping.Web.Razor.Tests.Support;

public static class TestData
{
    public static readonly Guid ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CustomerId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static ProductModel Product(string name = "Phone") => new()
    {
        Id = ProductId,
        Name = name,
        Category = ["Smart Phone"],
        Description = "Test product",
        ImageUrl = "https://example.com/product.png",
        Price = 99.99m
    };

    public static ShoppingCartModel Cart(string userName = "swn") => new()
    {
        UserName = userName,
        Items =
        [
            new ShoppingCartItemModel
            {
                ProductId = ProductId,
                ProductName = "Phone",
                Price = 99.99m,
                Quantity = 1,
                Color = "Black"
            }
        ]
    };

    public static DevUserContext DevUser(string userName = "swn") => new()
    {
        UserName = userName,
        CustomerId = CustomerId
    };

    public static BasketCheckoutModel Checkout() => new()
    {
        FirstName = "Test",
        LastName = "User",
        EmailAddress = "test@example.com",
        AddressLine = "One Way",
        Country = "US",
        State = "WA",
        ZipCode = "98052",
        CardName = "Test User",
        CardNumber = "4242424242424242",
        Expiration = "12/30",
        CVV = "123",
        PaymentMethod = 1
    };

    public static OrderModel Order() => new(
        Guid.NewGuid(),
        CustomerId,
        "ORD-1",
        new AddressModel("Test", "User", "test@example.com", "One Way", "US", "WA", "98052"),
        new AddressModel("Test", "User", "test@example.com", "One Way", "US", "WA", "98052"),
        new PaymentModel("Test", "424242", "12/30", "123", 1),
        OrderStatus.Pending,
        [new OrderItemModel(Guid.NewGuid(), ProductId, 1, 99.99m)]);
}
