namespace Ordering.API.Endpoints
{
    public static class OrdersEndpoints
    {
        public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/orders")
                .WithTags("Orders");

            group.MapGetOrders();
            group.MapGetOrdersByName();
            group.MapGetOrdersByCustomer();
            group.MapCreateOrder();
            group.MapUpdateOrder();
            group.MapDeleteOrder();

            return app;
        }
    }
}
