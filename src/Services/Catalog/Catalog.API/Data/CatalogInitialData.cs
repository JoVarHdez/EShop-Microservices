using Catalog.API.Models;
using Marten;
using Marten.Schema;

namespace Catalog.API.Data
{
    public class CatalogInitialData : IInitialData
    {
        public async Task Populate(IDocumentStore store, CancellationToken cancellation)
        {
            using var session = store.LightweightSession();

            if (await session.Query<Product>().AnyAsync(cancellation))
            {
                return;
                // When you want to clear existing data before seeding new data
                // remove the return statement and uncomment the line below to delete all products.
                // session.DeleteWhere<Product>(_ => true);
            }

            session.Store<Product>(GetPreconfigProds());
            await session.SaveChangesAsync(cancellation);
        }

        private static IEnumerable<Product> GetPreconfigProds()
        {
            return [
                new Product
                {
                    Id = new Guid("00000000-0000-0000-0000-000000000001"),
                    Name = "Product 1",
                    Description = "Description for product 1",
                    Price = 10.99m,
                    ImageUrl = "product-1.png",
                    Categories = ["Category1", "Category2"]
                },
                new Product
                {
                    Id = new Guid("00000000-0000-0000-0000-000000000002"),
                    Name = "Product 2",
                    Description = "Description for product 2",
                    Price = 19.99m,
                    ImageUrl = "product-2.png",
                    Categories = ["Category2", "Category3"]
                },
                new Product
                {
                    Id = new Guid("00000000-0000-0000-0000-000000000003"),
                    Name = "Product 3",
                    Description = "Description for product 3",
                    Price = 5.99m,
                    ImageUrl = "product-3.png",
                    Categories = ["Category1"]
                },
                new Product
                {
                    Id = new Guid("00000000-0000-0000-0000-000000000004"),
                    Name = "Product 4",
                    Description = "Description for product 4",
                    Price = 15.49m,
                    ImageUrl = "product-4.png",
                    Categories = ["Category3"]
                },
                new Product
                {
                    Id = new Guid("00000000-0000-0000-0000-000000000005"),
                    Name = "Product 5",
                    Description = "Description for product 5",
                    Price = 8.75m,
                    ImageUrl = "product-5.png",
                    Categories = ["Category1", "Category2"]
                },
                new Product
                {
                    Id = new Guid("00000000-0000-0000-0000-000000000006"),
                    Name = "Product 6",
                    Description = "Description for product 6",
                    Price = 12.99m,
                    ImageUrl = "product-6.png",
                    Categories = ["Category2"]
                },
                new Product
                {
                    Id = new Guid("00000000-0000-0000-0000-000000000007"),
                    Name = "Product 7",
                    Description = "Description for product 7",
                    Price = 9.99m,
                    ImageUrl = "product-7.png",
                    Categories = ["Category1"]
                }];
        }
    }
}
