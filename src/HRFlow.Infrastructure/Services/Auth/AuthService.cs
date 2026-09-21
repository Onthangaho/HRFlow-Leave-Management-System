using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HRFlow.Domain.DTOs.Auth;
using HRFlow.Domain.Interfaces.Auth;
using HRFlow.Domain.Models.Auth;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace HRFlow.Infrastructure.Services.Auth;

/// <summary>
/// Implements login and refresh-token rotation using ASP.NET Identity credentials, JWT issuance, and server-side refresh token storage.
/// </summary>
public sealed class AuthService : IAuthService
{
    private const int DefaultAccessTokenMinutes = 15;
    private const int DefaultRefreshTokenDays = 7;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly HRFlowDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Creates a new auth service with identity, persistence, and configuration dependencies.
    /// </summary>
    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        HRFlowDbContext dbContext,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            _logger.LogWarning("Login failed because the supplied account was not found.");
            return AuthResult.Failed(AuthFailureReason.InvalidCredentials);
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            _logger.LogWarning(
                "Login failed. UserId: {UserId}; IsLockedOut: {IsLockedOut}; IsNotAllowed: {IsNotAllowed}",
                user.Id,
                signInResult.IsLockedOut,
                signInResult.IsNotAllowed);
            return AuthResult.Failed(AuthFailureReason.InvalidCredentials);
        }

        var tokenResponse = await IssueTokenPairAsync(user, cancellationToken);
        _logger.LogInformation("Login succeeded. UserId: {UserId}", user.Id);
        return AuthResult.Success(tokenResponse);
    }

    /// <inheritdoc />
    public async Task<AuthResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var tokenHash = HashRefreshToken(request.RefreshToken);

        // Atomically revoke the token using a conditional update that requires the token to be unrevoked and unexpired
        var rowsUpdated = await _dbContext.RefreshTokens
            .Where(rt =>
                rt.TokenHash == tokenHash
                && rt.RevokedAtUtc == null
                && rt.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(rt => rt.RevokedAtUtc, now),
                cancellationToken);

        // Proceed only if exactly one row was updated; otherwise the token was invalid, already used, or expired
        if (rowsUpdated != 1)
        {
            _logger.LogWarning("Refresh token exchange failed because the supplied token was invalid, expired, or already revoked.");
            return AuthResult.Failed(AuthFailureReason.InvalidRefreshToken);
        }

        // Retrieve the revoked token to get the user ID
        var revokedToken = await _dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (revokedToken is null)
        {
            _logger.LogWarning("Refresh token exchange failed because the revoked token record could not be retrieved.");
            return AuthResult.Failed(AuthFailureReason.InvalidRefreshToken);
        }

        var user = await _userManager.FindByIdAsync(revokedToken.UserId);
        if (user is null)
        {
            _logger.LogWarning(
                "Refresh token exchange failed because the token owner no longer exists. UserId: {UserId}",
                revokedToken.UserId);
            return AuthResult.Failed(AuthFailureReason.InvalidRefreshToken);
        }

        var tokenResponse = await IssueTokenPairAsync(user, cancellationToken);
        _logger.LogInformation("Refresh token exchange succeeded. UserId: {UserId}", user.Id);
        return AuthResult.Success(tokenResponse);
    }

    private async Task<TokenResponse> IssueTokenPairAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var accessTokenLifetime = TimeSpan.FromMinutes(GetAccessTokenMinutes());
        var accessTokenExpiresAtUtc = now.Add(accessTokenLifetime);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = CreateAccessToken(user, roles, accessTokenExpiresAtUtc);

        var refreshTokenValue = CreateRefreshTokenValue();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id.ToString(),
            TokenHash = HashRefreshToken(refreshTokenValue),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(GetRefreshTokenDays())
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Token pair issued. UserId: {UserId}; RoleCount: {RoleCount}; AccessTokenExpiresAtUtc: {AccessTokenExpiresAtUtc}; RefreshTokenExpiresAtUtc: {RefreshTokenExpiresAtUtc}",
            user.Id,
            roles.Count,
            accessTokenExpiresAtUtc,
            refreshToken.ExpiresAtUtc);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc
        };
    }

    private string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles, DateTime expiresAtUtc)
    {
        var signingKey = GetSigningKey();
        var credentials = new SigningCredentials(new SymmetricSecurityKey(signingKey), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var jwt = new JwtSecurityToken(
            issuer: _configuration["Authentication:Jwt:Issuer"],
            audience: _configuration["Authentication:Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private byte[] GetSigningKey()
    {
        var signingKey = _configuration["Authentication:Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:SigningKey must be configured via appsettings or user secrets before issuing JWT tokens.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:SigningKey must be at least 32 bytes (256 bits) when UTF-8 encoded. " +
                $"Current key length is {keyBytes.Length} bytes.");
        }

        return keyBytes;
    }

    private int GetAccessTokenMinutes()
    {
        var configuredMinutes = _configuration["Authentication:Jwt:AccessTokenMinutes"];
        return int.TryParse(configuredMinutes, out var minutes)
            ? minutes
            : DefaultAccessTokenMinutes;
    }

    private int GetRefreshTokenDays()
    {
        var configuredDays = _configuration["Authentication:Jwt:RefreshTokenDays"];
        return int.TryParse(configuredDays, out var days)
            ? days
            : DefaultRefreshTokenDays;
    }

    private static string CreateRefreshTokenValue()
    {
        Span<byte> tokenBytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }

    private string HashRefreshToken(string token)
    {
        var pepper = _configuration["Authentication:Jwt:RefreshTokenPepper"];
        if (string.IsNullOrWhiteSpace(pepper))
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:RefreshTokenPepper must be configured via appsettings or user secrets for secure token hashing.");
        }

        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var pepperBytes = Encoding.UTF8.GetBytes(pepper);
        var combined = new byte[tokenBytes.Length + pepperBytes.Length];
        Buffer.BlockCopy(tokenBytes, 0, combined, 0, tokenBytes.Length);
        Buffer.BlockCopy(pepperBytes, 0, combined, tokenBytes.Length, pepperBytes.Length);

        var hashBytes = SHA256.HashData(combined);
        return Convert.ToBase64String(hashBytes);
    }
}