using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoanDecisionApi.Models.Domain;
using LoanDecisionApi.Models.DTO;
using Microsoft.IdentityModel.Tokens;

namespace LoanDecisionApi.Services;

public class JwtTokenService(IConfiguration configuration)
{
    public TokenResponse GenerateToken(ApiPartner partner)
    {
        var signingKey = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Configuration value 'Jwt:SigningKey' is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Configuration value 'Jwt:Issuer' is not configured.");
        var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Configuration value 'Jwt:Audience' is not configured.");

        var key = new SymmetricSecurityKey(Convert.FromBase64String(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, partner.Id.ToString()),
            new Claim("name", partner.Name)
        };

        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiresAt, signingCredentials: credentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenResponse { AccessToken = accessToken, ExpiresAt = expiresAt };
    }
}
