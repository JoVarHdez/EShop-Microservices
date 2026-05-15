using Catalog.API.Exceptions.Handler;
using Catalog.API.Data;
using Catalog.API.Products;
using FluentValidation;
using HealthChecks.UI.Client;
using Marten;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);
var assembly = typeof(Program).Assembly;

// Add services to the container.
builder.Services.AddValidatorsFromAssembly(assembly);

builder.Services.AddMarten(config =>
{
    config.Connection(builder.Configuration.GetConnectionString("Database")!);
}).UseLightweightSessions()
.IntegrateWithWolverine();

if (builder.Environment.IsDevelopment())
{
    builder.Services.InitializeMartenWith<CatalogInitialData>();
}

builder.Services.AddExceptionHandler<CustomExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);

builder.Host.UseWolverine(opts => opts.UseFluentValidation(RegistrationBehavior.ExplicitRegistration));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapProductsEndpoints();

app.UseExceptionHandler(options => 
{ 

});

app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

await app.RunAsync();
