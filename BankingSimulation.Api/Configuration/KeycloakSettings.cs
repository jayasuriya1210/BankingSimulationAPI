namespace BankingSimulation.Api.Configuration;

public class KeycloakSettings
{
    public string AuthServerUrl     { get; set; } = null!;
    public string Realm             { get; set; } = null!;
    public string ClientId          { get; set; } = null!;
    public string ClientSecret      { get; set; } = null!;
    public string AdminClientId     { get; set; } = null!;
    public string AdminClientSecret { get; set; } = null!;

    public string TokenUrl      => $"{AuthServerUrl}/realms/{Realm}/protocol/openid-connect/token";
    public string AdminTokenUrl => $"{AuthServerUrl}/realms/master/protocol/openid-connect/token";
    public string UsersUrl      => $"{AuthServerUrl}/admin/realms/{Realm}/users";
    public string Issuer        => $"{AuthServerUrl}/realms/{Realm}";
    public string JwksUri       => $"{AuthServerUrl}/realms/{Realm}/protocol/openid-connect/certs";
    public string UserInfoUrl   => $"{AuthServerUrl}/realms/{Realm}/protocol/openid-connect/userinfo";
}
