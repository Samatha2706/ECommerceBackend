using System.Net;
using System.Text.Json;

namespace ECommerce.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (KeyNotFoundException ex)
        {
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.NotFound);
        }
        catch (ArgumentException ex)
        {
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.BadRequest);
        }
        catch (InvalidOperationException ex)
        {
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.BadRequest);
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.Unauthorized);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unexpected error occurred.");

            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.InternalServerError);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        HttpStatusCode statusCode)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            statusCode = (int)statusCode,
            message = exception.Message
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}