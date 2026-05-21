using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;

namespace Ordering.Infrastructure.Data.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Id).HasConversion(id => id.Value, value => OrderItemId.Of(value));

            
            builder.HasOne<Product>().WithMany().HasForeignKey(item => item.ProductId);

            builder.Property(item => item.Quantity).IsRequired();
            builder.Property(item => item.Price).IsRequired();
        }
    }
}
