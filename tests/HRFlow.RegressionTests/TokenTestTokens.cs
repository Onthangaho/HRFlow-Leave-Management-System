using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HRFlow.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace HRFlow.RegressionTests;

/// <summary>Signs synthetic bearer probes with a private fixture key, never changing production issuance or validation.</summary>
internal static class TokenTestTokens
{
    internal static string Create(ApiFixture fixture, string actor, DateTime? expires = null, DateTime? notBefore = null,
        string? issuer = null, string? audience = null, bool wrongKey = false, string algorithm = SecurityAlgorithms.HmacSha256,
        bool omitExpiry = false, bool omitVersion = false, bool staleVersion = false)
    {
        var settings = fixture.AuthenticationEnvironment();
        var user = fixture.User(actor);
        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), new(ClaimTypes.Role, "Employee") };
        if (!omitVersion) claims.Add(new(CredentialClaims.Version, (staleVersion ? Guid.NewGuid() : user.CredentialVersion).ToString()));
        var signingKey = wrongKey ? new string('X', 96) : settings["Authentication__Jwt__SigningKey"];
        var jwt = new JwtSecurityToken(issuer ?? settings["Authentication__Jwt__Issuer"],
            audience ?? settings["Authentication__Jwt__Audience"], claims,
            notBefore ?? DateTime.UtcNow.AddMinutes(-10), omitExpiry ? null : expires ?? DateTime.UtcNow.AddMinutes(10),
            algorithm == SecurityAlgorithms.None ? null : new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), algorithm));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
