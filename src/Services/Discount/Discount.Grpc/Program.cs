using Discount.Grpc.Data;
using Discount.Grpc.Interceptors;
using Discount.Grpc.Repository;
using Discount.Grpc.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ValidationInterceptor>();
});
builder.Services.AddGrpcReflection();
builder.Services.AddDbContext<DiscountContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Database")));

var assembly = typeof(Program).Assembly;
builder.Services.AddValidatorsFromAssembly(assembly);
builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DiscountContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMigration();
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}
app.MapGrpcService<DiscountService>();
app.MapHealthChecks("/health");
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

await app.RunAsync();
