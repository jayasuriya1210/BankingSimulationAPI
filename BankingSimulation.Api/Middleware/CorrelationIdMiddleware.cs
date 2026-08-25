namespace BankingSimulation.Api.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId) ||
            string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N")[..16];
        }

        context.Items[HeaderName] = correlationId.ToString();
        context.Response.Headers[HeaderName] = correlationId.ToString();

        await next(context);
    }
}
