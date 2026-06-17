using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ordering.Core.Enums;
using Ordering.Core.ValueObjects;
using Ordering.Core.Models;
using Ordering.Infrastructure.Data;
using Ordering.Infrastructure.Extensions;
using Testcontainers.MsSql;

namespace Ordering.Tests.Support;

public sealed class OrderingSqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

    public string ConnectionString
    {
        get
        {
            var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
            {
                InitialCatalog = "OrderingTests"
            };

            return builder.ConnectionString;
        }
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await CreateDatabaseIfNotExistsAsync();
        await ResetAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task<ApplicationDbContext> CreateDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.CloseConnectionAsync();

        return dbContext;
    }

    public async Task ResetAsync()
    {
        await using var dbContext = await CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async Task SeedAsync(params Order[] orders)
    {
        await using var dbContext = await CreateDbContextAsync();

        if (!await dbContext.Customers.AnyAsync())
        {
            dbContext.Customers.AddRange(InitialData.Customers);
        }

        if (!await dbContext.Products.AnyAsync())
        {
            dbContext.Products.AddRange(InitialData.Products);
        }

        if (orders.Length > 0)
        {
            dbContext.Orders.AddRange(orders);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task SeedInitialOrdersAsync()
    {
        await using var dbContext = await CreateDbContextAsync();

        if (!await dbContext.Customers.AnyAsync())
        {
            dbContext.Customers.AddRange(InitialData.Customers);
        }

        if (!await dbContext.Products.AnyAsync())
        {
            dbContext.Products.AddRange(InitialData.Products);
        }

        if (!await dbContext.Orders.AnyAsync())
        {
            foreach (var order in InitialData.OrderWithItems)
            {
                order.Update(
                    order.OrderName,
                    order.ShippingAddress,
                    order.BillingAddress,
                    order.Payment,
                    OrderStatus.Pending);

                dbContext.Orders.Add(order);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<Guid> EnsureDefaultCustomerAsync()
    {
        var defaultCustomerId = new Guid("00000000-0000-0000-0000-000000000001");

        await using var dbContext = await CreateDbContextAsync();

        var exists = await dbContext.Customers.AnyAsync(x => x.Id == CustomerId.Of(defaultCustomerId));
        if (!exists)
        {
            dbContext.Customers.Add(Customer.Create(CustomerId.Of(defaultCustomerId), "John Doe", "johndoe@example.com"));
            await dbContext.SaveChangesAsync();
        }

        return defaultCustomerId;
    }

    public async Task<Guid> EnsureDefaultProductAsync()
    {
        var defaultProductId = new Guid("5334c996-8457-4cf0-815c-ed2b77c4ff61");

        await using var dbContext = await CreateDbContextAsync();

        var exists = await dbContext.Products.AnyAsync(x => x.Id == ProductId.Of(defaultProductId));
        if (!exists)
        {
            dbContext.Products.Add(Product.Create(ProductId.Of(defaultProductId), "IPhone X", 500m));
            await dbContext.SaveChangesAsync();
        }

        return defaultProductId;
    }

    private async Task CreateDatabaseIfNotExistsAsync()
    {
        var adminConnectionString = _container.GetConnectionString();

        await using var connection = new SqlConnection(adminConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "IF DB_ID(N'OrderingTests') IS NULL CREATE DATABASE [OrderingTests];";
        await command.ExecuteNonQueryAsync();
    }
}