namespace BankingSimulation.Api.Models;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public object? Data { get; set; }
    public string? Error { get; set; }
    public string? CorrelationId { get; set; }

    public static ApiResponse Ok(string message, object? data = null, string? correlationId = null) =>
        new() { Success = true, Message = message, Data = data, CorrelationId = correlationId };

    public static ApiResponse Fail(string error, string? correlationId = null) =>
        new() { Success = false, Message = "Request failed", Error = error, CorrelationId = correlationId };
}
