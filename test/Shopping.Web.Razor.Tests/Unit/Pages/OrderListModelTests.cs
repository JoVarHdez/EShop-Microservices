using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shopping.Web.Razor.Models.Ordering;
using Shopping.Web.Razor.Pages;
using Shopping.Web.Razor.Services;
using Shopping.Web.Razor.Tests.Support;

namespace Shopping.Web.Razor.Tests.Unit.Pages;

public class OrderListModelTests
{
    [Fact]
    public async Task OnGetAsync_UsesCurrentUserCustomerIdAndReturnsPage()
    {
        var user = new Mock<IDevUserContextProvider>();
        user.Setup(x => x.GetCurrent()).Returns(TestData.DevUser());

        var ordering = new Mock<IOrderingService>();
        ordering.Setup(x => x.GetOrdersByCustomerAsync(TestData.CustomerId))
            .ReturnsAsync(new GetOrdersByCustomerResponse([TestData.Order()]));

        var sut = new OrderListModel(ordering.Object, user.Object, NullLogger<OrderListModel>.Instance);

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Single(sut.Orders);
        ordering.Verify(x => x.GetOrdersByCustomerAsync(TestData.CustomerId), Times.Once);
    }
}
