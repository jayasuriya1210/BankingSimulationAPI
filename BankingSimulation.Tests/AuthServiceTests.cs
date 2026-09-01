using BankingSimulation.Api.DTOs;
using BankingSimulation.Api.Services;
using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;
using Moq;
using System.Net;

namespace BankingSimulation.Tests;

public class AuthServiceTests
{
    private readonly Mock<IKeycloakClient> _keycloak = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();
    private readonly AuthService _sut;

    // A minimal valid-looking JWT with sub, email, name claims (not signature-verified in unit tests)
    private const string FakeJwt =
        "eyJhbGciOiJSUzI1NiJ9." +
        "eyJzdWIiOiJ0ZXN0LXN1YiIsImVtYWlsIjoidGVzdEBiYW5rLmNvbSIsIm5hbWUiOiJUZXN0IFVzZXIiLCJyb2xlcyI6WyJDdXN0b21lciJdfQ." +
        "signature";

    public AuthServiceTests()
    {
        var auditService = new AuditService(_auditRepo.Object);
        _sut = new AuthService(_keycloak.Object, _userRepo.Object, auditService);
        _auditRepo.Setup(r => r.CreateAsync(It.IsAny<BankingSimulation.Data.Entities.AuditLog>()))
                  .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ThrowsUnauthorized()
    {
        _keycloak.Setup(k => k.GetTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                 .ReturnsAsync((KeycloakTokenResult?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(new LoginRequest("bad@bank.com", "wrong"), null));
    }

    [Fact]
    public async Task Login_DisabledUser_ThrowsUnauthorized()
    {
        _keycloak.Setup(k => k.GetTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                 .ReturnsAsync(new KeycloakTokenResult(FakeJwt, "refresh"));

        var user = new BankUser { Id = 1, KeycloakSubject = "test-sub", Email = "test@bank.com",
            DisplayName = "Test", Role = "Customer", IsActive = false };
        _userRepo.Setup(r => r.GetByKeycloakSubjectAsync("test-sub")).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(new LoginRequest("test@bank.com", "pass"), null));
    }

    [Fact]
    public async Task Login_ExistingEmailWithChangedKeycloakSubject_ReLinksLocalUser()
    {
        _keycloak.Setup(k => k.GetTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                 .ReturnsAsync(new KeycloakTokenResult(FakeJwt, "refresh"));

        var user = new BankUser
        {
            Id = 7,
            KeycloakSubject = "old-subject",
            Email = "test@bank.com",
            DisplayName = "Old Name",
            Role = "Customer",
            IsActive = true
        };
        _userRepo.Setup(r => r.GetByKeycloakSubjectAsync("test-sub")).ReturnsAsync((BankUser?)null);
        _userRepo.Setup(r => r.GetByEmailAsync("test@bank.com")).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

        var result = await _sut.LoginAsync(new LoginRequest("test@bank.com", "pass"), null);

        Assert.Equal(7, result.UserId);
        Assert.Equal("test-sub", user.KeycloakSubject);
        Assert.Equal("Test User", user.DisplayName);
        _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Signup_DuplicateEmail_ThrowsInvalidOperation()
    {
        _keycloak.Setup(k => k.GetAdminTokenAsync()).ReturnsAsync("admin-token");
        _keycloak.Setup(k => k.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((HttpStatusCode.Conflict, (string?)null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.SignupAsync(new SignupRequest("Test User", "dup@bank.com", "Pass@123"), null));
    }

    [Fact]
    public async Task Signup_LocalDuplicateEmail_ThrowsBeforeCreatingKeycloakUser()
    {
        var user = new BankUser
        {
            Id = 8,
            KeycloakSubject = "existing-subject",
            Email = "dup@bank.com",
            DisplayName = "Existing User",
            Role = "Customer",
            IsActive = true
        };
        _userRepo.Setup(r => r.GetByEmailAsync("dup@bank.com")).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.SignupAsync(new SignupRequest("Test User", "dup@bank.com", "Pass@123"), null));

        _keycloak.Verify(k => k.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Signup_LocalUserAppearsAfterKeycloakCreate_ReLinksExistingUser()
    {
        var user = new BankUser
        {
            Id = 9,
            KeycloakSubject = "old-subject",
            Email = "test@bank.com",
            DisplayName = "Old User",
            Role = "Customer",
            IsActive = false
        };

        _userRepo.SetupSequence(r => r.GetByEmailAsync("test@bank.com"))
            .ReturnsAsync((BankUser?)null)
            .ReturnsAsync(user);
        _keycloak.Setup(k => k.GetAdminTokenAsync()).ReturnsAsync("admin-token");
        _keycloak.Setup(k => k.CreateUserAsync("admin-token", "test@bank.com", "Test User", "Pass@123"))
            .ReturnsAsync((HttpStatusCode.Created, "test-sub"));
        _keycloak.Setup(k => k.AssignRealmRoleAsync("admin-token", "test-sub", "Customer"))
            .Returns(Task.CompletedTask);
        _keycloak.Setup(k => k.GetTokenAsync("test@bank.com", "Pass@123"))
            .ReturnsAsync(new KeycloakTokenResult(FakeJwt, "refresh"));
        _userRepo.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

        var result = await _sut.SignupAsync(new SignupRequest("Test User", "test@bank.com", "Pass@123"), null);

        Assert.Equal(9, result.UserId);
        Assert.Equal("test-sub", user.KeycloakSubject);
        Assert.Equal("Test User", user.DisplayName);
        Assert.True(user.IsActive);
        _userRepo.Verify(r => r.CreateAsync(It.IsAny<BankUser>()), Times.Never);
        _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Signup_NoAdminToken_ThrowsInvalidOperation()
    {
        _keycloak.Setup(k => k.GetAdminTokenAsync()).ReturnsAsync((string?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.SignupAsync(new SignupRequest("Test User", "new@bank.com", "Pass@123"), null));
    }

    [Fact]
    public async Task Refresh_InvalidToken_ThrowsUnauthorized()
    {
        _keycloak.Setup(k => k.RefreshTokenAsync(It.IsAny<string>()))
                 .ReturnsAsync((KeycloakTokenResult?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.RefreshAsync("bad-refresh-token", null));
    }
}
