using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Auth;
using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using Faaz.Services.Identity.Infrastructure.Services;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.SharedKernel.Security;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Commands;

public class RefreshTokenCommand : IRequest<AuthResponseDto>
{
    public required string RefreshToken { get; init; }
    public string? IpAddress { get; set; }
}

internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly IRefreshTokenServices _refreshTokenServices;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IHttpContextAccessor httpContext,
        IRefreshTokenServices refreshTokenServices,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ITokenService tokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _httpContext = httpContext;
        _refreshTokenServices = refreshTokenServices;
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        var ctx = _httpContext.HttpContext!;

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
            throw new UnauthorizedAccessException("Refresh token missing.");

        var hash = HashToken(command.RefreshToken);
        var existing = await _refreshTokenServices.GetByTokenHashAsync(hash, ct);

        if (existing is null || existing.IsRevoked)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (existing.IsUsed)
        {
            var ip = command.IpAddress ?? ctx.Connection.RemoteIpAddress?.ToString();
            await _refreshTokenServices.RevokeAllForUserAsync(existing.UserId, ip, ct);
            await _refreshTokenServices.SaveChangesAsync(ct);
            _logger.LogWarning("Refresh token reuse detected for UserId: {UserId}", existing.UserId);
            throw new UnauthorizedAccessException("security-breach");
        }

        if (existing.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired.");

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null)
            throw new UnauthorizedAccessException("User not found.");

        var permissions = await PermissionResolver.GetPermissionsAsync(_userManager, _roleManager, user);
        var newAccessToken = _tokenService.GenerateAccessToken(user, out var newJti, permissions);
        var (newRtPlaintext, newRtHash) = _tokenService.GenerateRefreshToken();
        var clientIp = command.IpAddress ?? ctx.Connection.RemoteIpAddress?.ToString();

        existing.Token       = newRtHash;
        existing.JwtId       = newJti;
        // Preserve the original "remember me" choice through rotation rather than resetting
        // to a fixed window — otherwise a non-remembered session would quietly become persistent.
        existing.ExpiresAt   = DateTime.UtcNow.AddDays(existing.RememberMe ? 30 : 1);
        existing.CreatedByIp = clientIp;

        await _refreshTokenServices.SaveChangesAsync(ct);

        _logger.LogInformation("Token refreshed for UserId: {UserId}", user.Id);

        return new AuthResponseDto(AccessToken: newAccessToken, RefreshToken: newRtPlaintext, RememberMe: existing.RememberMe);
    }

    private static string HashToken(string token) => TokenHasher.Hash(token);
}
