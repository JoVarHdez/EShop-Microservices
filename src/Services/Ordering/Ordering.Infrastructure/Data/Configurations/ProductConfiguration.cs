using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;

namespace Ordering.Infrastructure.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(prod => prod.Id);
            builder.Property(prod => prod.Id).HasConversion(id => id.Value, value => ProductId.Of(value));
            builder.Property(prod => prod.Name).IsRequired().HasMaxLength(100);
        }
    }
}
