namespace Basket.API.Models
{
    public class ShoppingCart
    {
        public string UserName { get; set; } = default!;
        public List<ShoppingCartItem> Items { get; set; } = [];
        public decimal TotalPrice => Items.Sum(i => i.Price * i.Quantity);

        public ShoppingCart(string userName)
        {
            UserName = userName;
        }

        // Required for Mapping package to work properly
        public ShoppingCart() { }
    }
}
