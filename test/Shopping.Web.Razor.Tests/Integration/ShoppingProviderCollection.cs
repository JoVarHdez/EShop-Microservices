namespace Shopping.Web.Razor.Tests.Integration;

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class ShoppingProviderCollection : ICollectionFixture<ShoppingProviderFixture>
{
    public const string CollectionName = "Shopping.Provider";
}

public sealed class ShoppingProviderFixture
{
}
