namespace Shopping.Web.Razor.Models.Catalog
{
    public class ProductModel
    {
        public Guid Id { get; set; } = Guid.Empty!;
        public string Name { get; set; } = default!;
        public List<string> Category { get; set; } = [];
        public string Description { get; set; } = default!;
        public string ImageUrl { get; set; } = default!;
        public decimal Price { get; set; } = default!;
    }

    public record GetProductsResponse(IEnumerable<ProductModel> Products);
    public record GetProductsByCategoryResponse(IEnumerable<ProductModel> Products);
    public record GetProductByIdResponse(ProductModel Product);
}
