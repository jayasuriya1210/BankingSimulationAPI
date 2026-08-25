namespace BankingSimulation.Api.Configuration;

// Retained for any future local signing needs; JWT validation now uses Keycloak JWKS.
public class JwtSettings
{
    public string Audience { get; set; } = null!;
}
