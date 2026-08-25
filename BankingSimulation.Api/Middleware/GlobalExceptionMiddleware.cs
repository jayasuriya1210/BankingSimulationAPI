using BankingSimulation.Api.Models;
using System.Text.Json;

namespace BankingSimulation.Api.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items[CorrelationIdMiddleware.HeaderName]?.ToString();
            logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);
            await WriteErrorAsync(context, ex, correlationId);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception ex, string? correlationId)
    {
        var (status, message) = ex switch
        {
            UnauthorizedAccessException => (401, ex.Message),
            KeyNotFoundException        => (404, ex.Message),
            InvalidOperationException   => (409, ex.Message),
            ArgumentException           => (400, ex.Message),
            _                           => (500, "An unexpected error occurred.")
        };

        context.Response.StatusCode  = status;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.Fail(message, correlationId);
        return context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
