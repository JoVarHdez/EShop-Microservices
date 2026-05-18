namespace Basket.API.Models
{
    public class ShoppingCartItem
    {
        public int Quantity { get; set; } = default;
        public string Color { get; set; } = default!;
        public decimal Price { get; set; } = default;
        public Guid ProductId { get; set; } = Guid.Empty;
        public string ProductName { get; set; } = default!;
    }
}
