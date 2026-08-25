using BankingSimulation.Api.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BankingSimulation.Api.Services;

public interface IKeycloakClient
{
    Task<KeycloakTokenResult?> GetTokenAsync(string email, string password);
    Task<KeycloakTokenResult?> RefreshTokenAsync(string refreshToken);
    Task<string?> GetAdminTokenAsync();
    Task<(HttpStatusCode Status, string? KeycloakUserId)> CreateUserAsync(string adminToken, string email, string displayName, string password);
    Task AssignRealmRoleAsync(string adminToken, string keycloakUserId, string roleName);
}

public record KeycloakTokenResult(string AccessToken, string RefreshToken);

public class KeycloakClient(
    IHttpClientFactory factory,
    KeycloakSettings settings,
    ILogger<KeycloakClient> logger) : IKeycloakClient
{
    private HttpClient Http => factory.CreateClient("Keycloak");

    public async Task<KeycloakTokenResult?> GetTokenAsync(string email, string password)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "password",
            ["client_id"]     = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["username"]      = email,
            ["password"]      = password,
            ["scope"]         = "openid"
        });

        var response = await Http.PostAsync(settings.TokenUrl, form);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Keycloak login failed for {Email}: {Status}", email, response.StatusCode);
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new KeycloakTokenResult(
            body.GetProperty("access_token").GetString()!,
            body.GetProperty("refresh_token").GetString()!);
    }

    public async Task<KeycloakTokenResult?> RefreshTokenAsync(string refreshToken)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "refresh_token",
            ["client_id"]     = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["refresh_token"] = refreshToken
        });

        var response = await Http.PostAsync(settings.TokenUrl, form);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Keycloak refresh failed: {Status}", response.StatusCode);
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new KeycloakTokenResult(
            body.GetProperty("access_token").GetString()!,
            body.GetProperty("refresh_token").GetString()!);
    }

    public async Task<string?> GetAdminTokenAsync()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "client_credentials",
            ["client_id"]     = settings.AdminClientId,
            ["client_secret"] = settings.AdminClientSecret
        });

        var response = await Http.PostAsync(settings.AdminTokenUrl, form);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Keycloak admin token failed: {Status}", response.StatusCode);
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("access_token").GetString();
    }

    public async Task<(HttpStatusCode Status, string? KeycloakUserId)> CreateUserAsync(
        string adminToken, string email, string displayName, string password)
    {
        var parts = displayName.Split(' ', 2);
        var payload = new
        {
            username    = email,
            email       = email,
            firstName   = parts[0],
            lastName    = parts.Length > 1 ? parts[1] : string.Empty,
            enabled     = true,
            credentials = new[] { new { type = "password", value = password, temporary = false } }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, settings.UsersUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        req.Content = JsonContent.Create(payload);

        var response = await Http.SendAsync(req);
        if (!response.IsSuccessStatusCode)
            return (response.StatusCode, null);

        var userId = response.Headers.Location?.ToString().Split('/').Last();
        return (response.StatusCode, userId);
    }

    public async Task AssignRealmRoleAsync(string adminToken, string keycloakUserId, string roleName)
    {
        var roleUrl = $"{settings.AuthServerUrl}/admin/realms/{settings.Realm}/roles/{roleName}";
        var roleReq = new HttpRequestMessage(HttpMethod.Get, roleUrl);
        roleReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var roleResp = await Http.SendAsync(roleReq);
        if (!roleResp.IsSuccessStatusCode)
        {
            logger.LogWarning("Keycloak role '{Role}' not found: {Status}", roleName, roleResp.StatusCode);
            return;
        }

        var role = await roleResp.Content.ReadFromJsonAsync<JsonElement>();

        var assignUrl = $"{settings.AuthServerUrl}/admin/realms/{settings.Realm}/users/{keycloakUserId}/role-mappings/realm";
        var assignReq = new HttpRequestMessage(HttpMethod.Post, assignUrl);
        assignReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        assignReq.Content = JsonContent.Create(new[] { role });
        await Http.SendAsync(assignReq);
    }
}
