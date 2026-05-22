using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Core.Enums;
using Ordering.Core.Models;
using Ordering.Core.ValueObjects;

namespace Ordering.Infrastructure.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(order => order.Id);
            builder.Property(order => order.Id).HasConversion(id => id.Value, value => OrderId.Of(value));

            builder.HasOne<Customer>().WithMany().HasForeignKey(order => order.CustomerId);
            builder.HasMany(order => order.OrderItems).WithOne().HasForeignKey(item => item.OrderId);

            builder.ComplexProperty(order => order.OrderName, nameBuilder => 
            { 
                nameBuilder.Property(orderName => orderName.Value)
                           .HasColumnName(nameof(Order.OrderName))
                           .HasMaxLength(100)
                           .IsRequired();
            });

            builder.ComplexProperty(order => order.BillingAddress, addressBuilder => 
            { 
                addressBuilder.Property(address => address.FirstName)
                           .HasMaxLength(100)
                           .IsRequired();

                addressBuilder.Property(address => address.LastName)
                           .HasMaxLength(50)
                           .IsRequired();

                addressBuilder.Property(address => address.EmailAddress)
                           .HasMaxLength(50)
                           .IsRequired();

                addressBuilder.Property(address => address.AddressLine)
                           .HasMaxLength(180)
                           .IsRequired();

                addressBuilder.Property(address => address.Country)
                           .HasMaxLength(50)
                           .IsRequired();

                addressBuilder.Property(address => address.State)
                           .HasMaxLength(50)
                           .IsRequired();

                addressBuilder.Property(address => address.ZipCode)
                           .HasMaxLength(5)
                           .IsRequired();
            });

            builder.ComplexProperty(order => order.ShippingAddress, addressBuilder => 
            { 
                addressBuilder.Property(address => address.FirstName)
                           .HasMaxLength(50)
                           .IsRequired();

                addressBuilder.Property(address => address.LastName)
                           .HasMaxLength(50)
                           .IsRequired();

                addressBuilder.Property(address => address.EmailAddress)
                           .HasMaxLength(50)
                           .IsRequired();

                addressBuilder.Property(address => address.AddressLine)
                           .HasMaxLength(180)
                           .IsRequired();

                addressBuilder.Property(address => address.Country)
                           .HasMaxLength(50)
                           .IsRequired();

                addressBuilder.Property(address => address.State)
                           .HasMaxLength(50)
                           .IsRequired();

                addressBuilder.Property(address => address.ZipCode)
                           .HasMaxLength(5)
                           .IsRequired();
            });

            builder.ComplexProperty(order => order.Payment, paymentBuilder => 
            { 
                paymentBuilder.Property(address => address.CardName)
                           .HasMaxLength(50);

                paymentBuilder.Property(address => address.CardNumber)
                           .HasMaxLength(24)
                           .IsRequired();

                paymentBuilder.Property(address => address.Expiration)
                           .HasMaxLength(10);

                paymentBuilder.Property(address => address.CVV)
                           .HasMaxLength(3);

                paymentBuilder.Property(address => address.PaymentMethod);
            });

            builder.Property(order => order.Status)
                   .HasDefaultValue(OrderStatus.Draft)
                   .HasConversion(status => status.ToString(), dbStatus => Enum.Parse<OrderStatus>(dbStatus));

            builder.Property(order => order.TotalAmount);
        }
    }
}
