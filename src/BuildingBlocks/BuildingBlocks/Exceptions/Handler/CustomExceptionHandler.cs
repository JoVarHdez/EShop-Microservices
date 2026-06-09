using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using DataAnnotationsValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace BuildingBlocks.Exceptions.Handler
{
    public class CustomExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (statusCode, title) = GetStatusAndTitle(exception);

            var problemDetails = new ProblemDetails
            {
                Detail = exception.Message,
                Title = title,
                Status = statusCode,
                Instance = httpContext.Request.Path
            };

            if (exception is ValidationException validationException)
            {
                problemDetails.Extensions["errors"] = validationException.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(x => x.ErrorMessage).ToArray());
            }

            problemDetails.Extensions.Add("traceId", httpContext.TraceIdentifier);

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }

        private static (int StatusCode, string Title) GetStatusAndTitle(Exception exception)
        {
            return exception switch
            {
                ValidationException => (StatusCodes.Status400BadRequest, "ValidationException"),
                DataAnnotationsValidationException => (StatusCodes.Status400BadRequest, "ValidationException"),
                _ => (StatusCodes.Status500InternalServerError, exception.GetType().Name)
            };
        }
    }
}
