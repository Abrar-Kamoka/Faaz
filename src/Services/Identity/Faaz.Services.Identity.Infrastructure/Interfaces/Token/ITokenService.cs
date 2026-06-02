using Faaz.Services.Identity.Domain.Entities;

namespace Faaz.Services.Identity.Infrastructure.Interfaces.Token;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, out string jti);
    (string plaintext, string hash) GenerateRefreshToken();
    (string plaintext, string hash) GenerateOpaqueToken();
    string GetJwksJson();
}
