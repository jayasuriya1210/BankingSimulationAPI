using System.Security.Claims;

namespace BankingSimulation.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    // "bank_user_id" is a custom claim we add via ClaimsTransformation after syncing the local user.
    // Falls back to NameIdentifier for compatibility.
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var val = user.FindFirstValue("bank_user_id")
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new UnauthorizedAccessException("User ID claim not found.");
        return long.Parse(val);
    }

    public static string GetRole(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role)
        ?? user.FindFirstValue("roles")
        ?? "Customer";
}
