using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace BankingSimulation.Api.Authorization;

public class BankingClaimsTransformer(
    IUserRepository userRepo,
    ILogger<BankingClaimsTransformer> logger) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue("sub")
                   ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (subject is null) return principal;

        if (principal.HasClaim(c => c.Type == "bank_user_id")) return principal;

        var user = await userRepo.GetByKeycloakSubjectAsync(subject);
        if (user is null)
        {
            var email = principal.FindFirstValue("email") ?? string.Empty;
            var name  = principal.FindFirstValue("name")  ?? email;
            var role  = ResolveRole(principal);

            user = new BankUser
            {
                KeycloakSubject = subject,
                Email           = email,
                DisplayName     = name,
                Role            = role,
                IsActive        = true,
                CreatedAtUtc    = DateTime.UtcNow
            };
            await userRepo.CreateAsync(user);
            logger.LogInformation("Auto-provisioned local user {Id} for Keycloak subject {Sub}", user.Id, subject);
        }

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("bank_user_id", user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));

        principal.AddIdentity(identity);
        return principal;
    }

    private static string ResolveRole(ClaimsPrincipal principal)
    {
        var direct = principal.Claims
            .Where(c => c.Type is "roles" or "role")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (direct.Contains("Admin"))    return "Admin";
        if (direct.Contains("Staff"))    return "Staff";
        if (direct.Contains("Customer")) return "Customer";

        var realmAccess = principal.FindFirstValue("realm_access");
        if (realmAccess is not null)
        {
            var doc   = JsonDocument.Parse(realmAccess);
            var roles = doc.RootElement.TryGetProperty("roles", out var r)
                ? r.EnumerateArray().Select(x => x.GetString() ?? string.Empty)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>();

            if (roles.Contains("Admin"))    return "Admin";
            if (roles.Contains("Staff"))    return "Staff";
            if (roles.Contains("Customer")) return "Customer";
        }

        return "Customer";
    }
}
