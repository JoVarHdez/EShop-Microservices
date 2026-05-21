using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;

namespace Ordering.Infrastructure.Data.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.HasKey(cus => cus.Id);
            builder.Property(cus => cus.Id).HasConversion(id => id.Value, value => CustomerId.Of(value));
            builder.Property(cus => cus.Name).IsRequired().HasMaxLength(100);
            builder.Property(cus => cus.Email).HasMaxLength(255);
            builder.HasIndex(cus => cus.Email).IsUnique();
        }
    }
}
