using BankingSimulation.Api.DTOs;
using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;
using System.IdentityModel.Tokens.Jwt;

namespace BankingSimulation.Api.Services;

public class AuthService(
    IKeycloakClient keycloak,
    IUserRepository userRepo,
    AuditService auditService)
{
    public async Task<TokenResponse> LoginAsync(LoginRequest req, string? correlationId)
    {
        var tokens = await keycloak.GetTokenAsync(req.Email, req.Password)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        var localUser = await SyncUserAsync(tokens.AccessToken);

        if (!localUser.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        await auditService.LogAsync(localUser.Id, "Login", "BankUser", localUser.Id.ToString(), null, "Success", correlationId);

        return new TokenResponse(tokens.AccessToken, tokens.RefreshToken, localUser.Role, localUser.Id);
    }

    public async Task<TokenResponse> SignupAsync(SignupRequest req, string? correlationId)
    {
        var adminToken = await keycloak.GetAdminTokenAsync()
            ?? throw new InvalidOperationException("Could not obtain Keycloak admin token.");

        var (status, keycloakUserId) = await keycloak.CreateUserAsync(adminToken, req.Email, req.DisplayName, req.Password);

        if (status == System.Net.HttpStatusCode.Conflict)
            throw new InvalidOperationException("Email already registered.");

        if (keycloakUserId is null)
            throw new InvalidOperationException($"Keycloak user creation failed with status: {status}.");

        await keycloak.AssignRealmRoleAsync(adminToken, keycloakUserId, "Customer");

        var user = new BankUser
        {
            KeycloakSubject = keycloakUserId,
            Email           = req.Email,
            DisplayName     = req.DisplayName,
            Role            = "Customer",
            IsActive        = true,
            CreatedAtUtc    = DateTime.UtcNow
        };
        await userRepo.CreateAsync(user);

        await auditService.LogAsync(user.Id, "Signup", "BankUser", user.Id.ToString(), null, "Success", correlationId);

        var tokens = await keycloak.GetTokenAsync(req.Email, req.Password)
            ?? throw new InvalidOperationException("Signup succeeded but login failed.");

        return new TokenResponse(tokens.AccessToken, tokens.RefreshToken, user.Role, user.Id);
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken, string? correlationId)
    {
        var tokens = await keycloak.RefreshTokenAsync(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var localUser = await SyncUserAsync(tokens.AccessToken);

        await auditService.LogAsync(localUser.Id, "RefreshToken", "BankUser", localUser.Id.ToString(), null, "Success", correlationId);

        return new TokenResponse(tokens.AccessToken, tokens.RefreshToken, localUser.Role, localUser.Id);
    }

    private async Task<BankUser> SyncUserAsync(string accessToken)
    {
        var jwt     = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var subject = jwt.Subject;
        var email   = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty;
        var name    = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? email;

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
            throw new UnauthorizedAccessException("Token is missing required user claims.");

        var user = await userRepo.GetByKeycloakSubjectAsync(subject);
        if (user is null)
        {
            // A recreated Keycloak user can have a new subject but the same
            // email. Re-link the existing local user instead of inserting a
            // duplicate email.
            user = await userRepo.GetByEmailAsync(email);
        }

        if (user is null)
        {
            // New users always get Customer role — Admin/Staff are pre-seeded only
            user = new BankUser
            {
                KeycloakSubject = subject,
                Email           = email,
                DisplayName     = name,
                Role            = "Customer",
                IsActive        = true,
                CreatedAtUtc    = DateTime.UtcNow
            };
            await userRepo.CreateAsync(user);
        }
        else
        {
            // For existing users, sync name/email but keep role from our DB — not from token
            if (user.KeycloakSubject != subject || user.Email != email || user.DisplayName != name)
            {
                user.KeycloakSubject = subject;
                user.Email       = email;
                user.DisplayName = name;
                await userRepo.UpdateAsync(user);
            }
        }

        return user;
    }

}
