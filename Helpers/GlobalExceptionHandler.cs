using Microsoft.AspNetCore.Diagnostics;

namespace FitBook_App.Helpers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is AppException appException)
        {
            httpContext.Response.StatusCode = appException.StatusCode;
            await httpContext.Response.WriteAsync(appException.Message, cancellationToken);
            return true;
        }

        _logger.LogError(exception, "An unexpected error occurred.");
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsync("An unexpected error occurred.", cancellationToken);
        return true;
    }
}
