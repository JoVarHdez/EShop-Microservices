namespace Shopping.Web.Razor.Models.Basket
{
    public class ShoppingCartModel
    {
        public string UserName { get; set; } = default!;
        public List<ShoppingCartItemModel> Items { get; set; } = [];
        public decimal TotalPrice => Items.Sum(i => i.Price * i.Quantity);
    }

    public class ShoppingCartItemModel
    {
        public Guid ProductId { get; set; } = Guid.Empty!;
        public string ProductName { get; set; } = default!;
        public decimal Price { get; set; } = default!;
        public int Quantity { get; set; } = default!;
        public string Color { get; set; } = default!;
    }

    public record GetBasketResponse(ShoppingCartModel Cart);
    public record StoreBasketRequest(ShoppingCartModel Cart);
    public record StoreBasketResponse(string UserName);
    public record DeleteBasketResponse(bool IsSuccess);
}
