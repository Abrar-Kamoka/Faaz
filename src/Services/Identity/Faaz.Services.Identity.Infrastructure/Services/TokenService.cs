using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using Faaz.SharedKernel.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using static Faaz.Services.Identity.Domain.IdentityEnums;

namespace Faaz.Services.Identity.Infrastructure.Services;

internal sealed class TokenService : ITokenService
{
    private readonly RsaSecurityKey _privateKey;
    private readonly RsaSecurityKey _publicKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;

    public TokenService(IConfiguration config)
    {
        var pem = config["Jwt:PrivateKeyPem"];
        if (string.IsNullOrWhiteSpace(pem))
            throw new InvalidOperationException(
                "Jwt:PrivateKeyPem is not configured. Set it via 'dotnet user-secrets set Jwt:PrivateKeyPem \"...\"' " +
                "for local dev, or the JWT__PRIVATEKEYPEM environment variable in other environments — it must never be committed to appsettings.json.");

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem.Replace("\\n", "\n"));
        _privateKey = new RsaSecurityKey(rsa) { KeyId = "faaz-rs256-v1" };

        var publicRsa = RSA.Create();
        publicRsa.ImportRSAPublicKey(rsa.ExportRSAPublicKey(), out _);
        _publicKey = new RsaSecurityKey(publicRsa) { KeyId = "faaz-rs256-v1" };

        _issuer = config["Jwt:Issuer"] ?? "faaz-identity";
        _audience = config["Jwt:Audience"] ?? "faaz-api";
        _accessTokenMinutes = int.TryParse(config["Jwt:AccessTokenMinutes"], out var m) ? m : 15;
    }

    public string GenerateAccessToken(ApplicationUser user, out string jti, IReadOnlyList<string>? permissions = null)
    {
        jti = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("userId", user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, jti),
            new("role", ((int)user.Role).ToString()),
        };

        if (user.Role == UserRole.Consultant && user.ConsultantApplicationStatus.HasValue)
            claims.Add(new("consultant_status", ((int)user.ConsultantApplicationStatus.Value).ToString()));

        // Additive, fine-grained gate on top of "role" — existing [Authorize(Policy = "AdminOnly")]
        // checks are untouched and keep working purely off the role claim above. Only a NEW policy
        // that explicitly opts into RequirePermission(...) will ever look at this.
        if (permissions is { Count: > 0 })
            foreach (var permission in permissions)
                claims.Add(new("permission", permission));

        var creds = new SigningCredentials(_privateKey, SecurityAlgorithms.RsaSha256);
        var expires = DateTime.UtcNow.AddMinutes(_accessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string plaintext, string hash) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var plaintext = Base64UrlEncoder.Encode(bytes);
        var hash = HashToken(plaintext);
        return (plaintext, hash);
    }

    public (string plaintext, string hash) GenerateOpaqueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var plaintext = Base64UrlEncoder.Encode(bytes);
        var hash = HashToken(plaintext);
        return (plaintext, hash);
    }

    public string GetJwksJson()
    {
        var rsaParams = _publicKey.Rsa.ExportParameters(false);
        var e = Base64UrlEncoder.Encode(rsaParams.Exponent!);
        var n = Base64UrlEncoder.Encode(rsaParams.Modulus!);

        var jwks = new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    alg = "RS256",
                    kid = _publicKey.KeyId,
                    n,
                    e
                }
            }
        };

        return JsonSerializer.Serialize(jwks);
    }

    private static string HashToken(string token) => TokenHasher.Hash(token);
}
