using Ordering.API;
using Ordering.Application;
using Ordering.Application.Orders.Commands.CreateOrder;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Extensions;
using Wolverine;
using Wolverine.FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddApiServices(builder.Configuration);

builder.Host.UseWolverine(opts =>
{
    opts.UseFluentValidation();
    opts.Discovery.IncludeAssembly(typeof(CreateOrderHandler).Assembly);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseApiServices();

await app.StartAsync();

if (app.Environment.IsDevelopment())
{
    await app.InitializeDatabaseAsync();
}

await app.WaitForShutdownAsync();
