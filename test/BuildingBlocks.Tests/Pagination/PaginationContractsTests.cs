using BuildingBlocks.Pagination;

namespace BuildingBlocks.Tests.Pagination;

public class PaginationContractsTests
{
    [Fact]
    public void PaginationRequest_ShouldUseDefaults_WhenNoValuesAreProvided()
    {
        var request = new PaginationRequest();

        Assert.Equal(0, request.PageIndex);
        Assert.Equal(10, request.PageSize);
    }

    [Fact]
    public void PaginationRequest_ShouldUseProvidedValues()
    {
        var request = new PaginationRequest(PageIndex: 2, PageSize: 25);

        Assert.Equal(2, request.PageIndex);
        Assert.Equal(25, request.PageSize);
    }

    [Fact]
    public void PaginatedResult_ShouldExposeConstructorValues()
    {
        var data = new[] { "a", "b" };

        var result = new PaginatedResult<string>(
            pageIndex: 3,
            pageSize: 20,
            count: 42,
            data: data);

        Assert.Equal(3, result.PageIndex);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(42, result.Count);
        Assert.Same(data, result.Data);
    }
}
