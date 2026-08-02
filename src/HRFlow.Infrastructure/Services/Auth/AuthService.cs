using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HRFlow.Application.DTOs.Auth;
using HRFlow.Application.Interfaces.Auth;
using HRFlow.Application.Models.Auth;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HRFlow.Infrastructure.Services.Auth;

/// <summary>
/// Implements login and refresh-token rotation using ASP.NET Identity credentials, JWT issuance, and server-side refresh token storage.
/// </summary>
public sealed class AuthService : IAuthService
{
    private const int DefaultAccessTokenMinutes = 15;
    private const int DefaultRefreshTokenDays = 7;

    private readonly UserManager<IdentityUser> _userManager;
    private readonly HRFlowDbContext _dbContext;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Creates a new auth service with identity, persistence, and configuration dependencies.
    /// </summary>
    public AuthService(UserManager<IdentityUser> userManager, HRFlowDbContext dbContext, IConfiguration configuration)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return AuthResult.Failed(AuthFailureReason.InvalidCredentials);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return AuthResult.Failed(AuthFailureReason.InvalidCredentials);
        }

        var tokenResponse = await IssueTokenPairAsync(user, cancellationToken);
        return AuthResult.Success(tokenResponse);
    }

    /// <inheritdoc />
    public async Task<AuthResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var existingRefreshToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(
                refreshToken =>
                    refreshToken.Token == request.RefreshToken
                    && refreshToken.RevokedAtUtc == null
                    && refreshToken.ExpiresAtUtc > now,
                cancellationToken);

        if (existingRefreshToken is null)
        {
            return AuthResult.Failed(AuthFailureReason.InvalidRefreshToken);
        }

        var user = await _userManager.FindByIdAsync(existingRefreshToken.UserId);
        if (user is null)
        {
            return AuthResult.Failed(AuthFailureReason.InvalidRefreshToken);
        }

        existingRefreshToken.RevokedAtUtc = now;
        var tokenResponse = await IssueTokenPairAsync(user, cancellationToken);
        return AuthResult.Success(tokenResponse);
    }

    private async Task<TokenResponse> IssueTokenPairAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var accessTokenLifetime = TimeSpan.FromMinutes(GetAccessTokenMinutes());
        var accessTokenExpiresAtUtc = now.Add(accessTokenLifetime);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = CreateAccessToken(user, roles, accessTokenExpiresAtUtc);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = CreateRefreshTokenValue(),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(GetRefreshTokenDays())
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc
        };
    }

    private string CreateAccessToken(IdentityUser user, IEnumerable<string> roles, DateTime expiresAtUtc)
    {
        var signingKey = GetSigningKey();
        var credentials = new SigningCredentials(new SymmetricSecurityKey(signingKey), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
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

        return Encoding.UTF8.GetBytes(signingKey);
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
}