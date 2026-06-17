using Ordering.Core.Exceptions;
using Ordering.Core.ValueObjects;

namespace Ordering.Tests.Unit.ValueObjects;

public class IdValueObjectsTests
{
    [Fact]
    public void OrderId_Of_WithNonEmptyGuid_ReturnsValueObject()
    {
        var id = Guid.NewGuid();

        var result = OrderId.Of(id);

        Assert.Equal(id, result.Value);
    }

    [Fact]
    public void OrderId_Of_WithEmptyGuid_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => OrderId.Of(Guid.Empty));
    }

    [Fact]
    public void OrderItemId_Of_WithNonEmptyGuid_ReturnsValueObject()
    {
        var id = Guid.NewGuid();

        var result = OrderItemId.Of(id);

        Assert.Equal(id, result.Value);
    }

    [Fact]
    public void OrderItemId_Of_WithEmptyGuid_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => OrderItemId.Of(Guid.Empty));
    }

    [Fact]
    public void CustomerId_Of_WithNonEmptyGuid_ReturnsValueObject()
    {
        var id = Guid.NewGuid();

        var result = CustomerId.Of(id);

        Assert.Equal(id, result.Value);
    }

    [Fact]
    public void CustomerId_Of_WithEmptyGuid_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CustomerId.Of(Guid.Empty));
    }

    [Fact]
    public void ProductId_Of_WithNonEmptyGuid_ReturnsValueObject()
    {
        var id = Guid.NewGuid();

        var result = ProductId.Of(id);

        Assert.Equal(id, result.Value);
    }

    [Fact]
    public void ProductId_Of_WithEmptyGuid_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ProductId.Of(Guid.Empty));
    }
}