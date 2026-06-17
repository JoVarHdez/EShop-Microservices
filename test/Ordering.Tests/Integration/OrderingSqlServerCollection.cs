using Ordering.Tests.Support;

namespace Ordering.Tests.Integration;

[CollectionDefinition("OrderingSqlServer", DisableParallelization = true)]
public sealed class OrderingSqlServerCollection : ICollectionFixture<OrderingSqlServerFixture>;