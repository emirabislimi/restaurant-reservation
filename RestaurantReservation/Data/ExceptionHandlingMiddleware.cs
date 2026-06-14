using System.Text.Json;
using RestaurantReservation.Data;

namespace RestaurantReservation.Data;

/// <summary>
/// Converts domain exceptions into clean ProblemDetails-style JSON responses,
/// so controllers/services can throw meaningful exceptions instead of returning
/// status codes everywhere.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                NotFoundException     => (StatusCodes.Status404NotFound, ex.Message),
                BusinessRuleException => (StatusCodes.Status400BadRequest, ex.Message),
                ForbiddenException    => (StatusCodes.Status403Forbidden, ex.Message),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized."),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                status,
                title,
                traceId = context.TraceIdentifier
            });
            await context.Response.WriteAsync(payload);
        }
    }
}
