using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VehicleExplorer.Application.Exceptions;

namespace VehicleExplorer.Api;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            InvalidVehicleQueryException invalid => (
                StatusCodes.Status400BadRequest,
                "Invalid vehicle query",
                invalid.Message),
            HttpRequestException => (
                StatusCodes.Status502BadGateway,
                "Vehicle data provider is unavailable",
                "Vehicle data provider is unavailable."),
            _ => (0, string.Empty, string.Empty)
        };

        if (status == 0)
        {
            return false;
        }

        logger.LogError(exception, "Vehicle request failed with status {StatusCode}", status);
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            },
            cancellationToken);

        return true;
    }
}
