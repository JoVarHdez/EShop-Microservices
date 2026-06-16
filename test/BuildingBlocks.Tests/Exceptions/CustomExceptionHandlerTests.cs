using System.Text.Json;
using BuildingBlocks.Exceptions.Handler;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using DataAnnotationsValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace BuildingBlocks.Tests.Exceptions;

public class CustomExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ShouldReturnBadRequest_WithValidationErrorsExtension()
    {
        var sut = new CustomExceptionHandler();
        var context = CreateHttpContext("/products");
        var exception = new FluentValidation.ValidationException(
            new[]
            {
                new ValidationFailure("Name", "Name is required"),
                new ValidationFailure("Name", "Name length is invalid"),
                new ValidationFailure("Price", "Price must be greater than zero")
            });

        var handled = await sut.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        var payload = await ReadProblemDetailsAsync(context);
        Assert.Equal("ValidationException", payload.RootElement.GetProperty("title").GetString());
        Assert.Equal("/products", payload.RootElement.GetProperty("instance").GetString());

        var errors = payload.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Name", out var nameErrors));
        Assert.Equal(2, nameErrors.GetArrayLength());
        Assert.True(errors.TryGetProperty("Price", out var priceErrors));
        Assert.Equal(1, priceErrors.GetArrayLength());
        Assert.True(payload.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnBadRequest_ForDataAnnotationsValidationException()
    {
        var sut = new CustomExceptionHandler();
        var context = CreateHttpContext("/orders");
        var exception = new DataAnnotationsValidationException("Data annotations validation failed");

        var handled = await sut.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        var payload = await ReadProblemDetailsAsync(context);
        Assert.Equal("ValidationException", payload.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnInternalServerError_ForUnhandledExceptions()
    {
        var sut = new CustomExceptionHandler();
        var context = CreateHttpContext("/health");
        var exception = new InvalidOperationException("boom");

        var handled = await sut.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        var payload = await ReadProblemDetailsAsync(context);
        Assert.Equal(nameof(InvalidOperationException), payload.RootElement.GetProperty("title").GetString());
        Assert.Equal("boom", payload.RootElement.GetProperty("detail").GetString());
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> ReadProblemDetailsAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
