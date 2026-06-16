using Catalog.API.Products.GetProductByCategory;
using Catalog.API.Products.GetProducts;

namespace Catalog.Tests.Unit.Endpoints;

public class GetProductsAndCategoryContractsTests
{
    [Fact]
    public void GetProductsRequest_DefaultPagination_UsesPage1Size10()
    {
        var request = new GetProductsRequest();

        Assert.Equal(1, request.PageNumber);
        Assert.Equal(10, request.PageSize);
    }

    [Fact]
    public void GetProductsRequest_AllowsNullPagination_WhenProvided()
    {
        var request = new GetProductsRequest(PageNumber: null, PageSize: null);

        Assert.Null(request.PageNumber);
        Assert.Null(request.PageSize);
    }

    [Fact]
    public void GetProductsByCategoryResponse_WithEmptyProducts_HasNoItems()
    {
        var response = new GetProductsByCategoryResponse([]);

        Assert.Empty(response.Products);
    }
}
