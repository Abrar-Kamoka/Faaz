using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Auth;
using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using Faaz.Services.Identity.Infrastructure.Services;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.Services.Identity.WebHost.HttpClients;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Faaz.Services.Identity.Domain.IdentityEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Commands;

public class LoginCommand : IRequest<AuthResponseDto>
{
    public LoginDto PostModel { get; set; } = null!;
    public string? IpAddress { get; set; }
}

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenServices _refreshTokenServices;
    private readonly IConsultantServiceClient _consultantClient;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ITokenService tokenService,
        IRefreshTokenServices refreshTokenServices,
        IConsultantServiceClient consultantClient,
        IHttpContextAccessor httpContext,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _refreshTokenServices = refreshTokenServices;
        _consultantClient = consultantClient;
        _httpContext = httpContext;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(command.PostModel.Email);

        if (user is not null && await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login blocked for {Email} — account locked out from repeated failures", command.PostModel.Email);
            throw new ForbiddenException("account-locked", "Too many failed login attempts. Please try again in a few minutes.");
        }

        if (user is null || !await _userManager.CheckPasswordAsync(user, command.PostModel.Password))
        {
            // Tracks AccessFailedCount and applies Identity's configured lockout (5 attempts / 5 min
            // by default) — CheckPasswordAsync alone never touches this, so without this call the
            // Lockout* columns just sit unused and login has no brute-force protection at all.
            if (user is not null)
                await _userManager.AccessFailedAsync(user);
            _logger.LogWarning("Login failed for {Email}", command.PostModel.Email);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        if (user.Status == UserStatus.Suspended)
            throw new ForbiddenException("account-suspended", "Your account has been suspended.");

        if (user.Role == UserRole.Consultant)
        {
            if (!user.IsEmailVerified)
                throw new ForbiddenException("email-not-verified", "Please verify your email address before logging in.");

            if (user.ConsultantApplicationStatus == ConsultantApplicationStatus.Rejected)
                throw new ForbiddenException("application-rejected", "Your application has been rejected.");

            // Phase 2: also block PendingRevision

            // Allow SettingUpProfile and Active. If SettingUpProfile, check whether the
            // Consultant service has already auto-activated the profile — if so, sync here.
            if (user.ConsultantApplicationStatus == ConsultantApplicationStatus.SettingUpProfile)
            {
                var isComplete = await _consultantClient.GetProfileIsCompleteAsync(user.Id, ct);
                if (isComplete)
                {
                    user.ConsultantApplicationStatus = ConsultantApplicationStatus.Active;
                    user.Status = UserStatus.Active;
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation("Consultant {UserId} promoted to Active on login", user.Id);
                }
            }
        }

        if (user.Role == UserRole.Student && !user.IsEmailVerified)
            throw new ForbiddenException("email-not-verified", "Please verify your email address before logging in.");

        var permissions = await PermissionResolver.GetPermissionsAsync(_userManager, _roleManager, user);
        var accessToken = _tokenService.GenerateAccessToken(user, out var jti, permissions);
        var (rtPlaintext, rtHash) = _tokenService.GenerateRefreshToken();
        var ip = command.IpAddress ?? _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();

        // "Remember me" controls how long the refresh token itself stays valid server-side.
        // The cookie's persistence (survives browser restart or not) is set separately in
        // AuthController.SetRefreshTokenCookie using the same flag.
        var expiresAt = DateTime.UtcNow.AddDays(command.PostModel.RememberMe ? 30 : 1);

        var existingToken = await _refreshTokenServices.GetByUserIdAsync(user.Id, ct);
        if (existingToken is not null)
        {
            existingToken.Token          = rtHash;
            existingToken.JwtId          = jti;
            existingToken.ExpiresAt      = expiresAt;
            existingToken.RememberMe     = command.PostModel.RememberMe;
            existingToken.CreatedByIp    = ip;
            existingToken.IsUsed         = false;
            existingToken.IsRevoked      = false;
            existingToken.ReplacedByToken = null;
            existingToken.RevokedByIp    = null;
        }
        else
        {
            await _refreshTokenServices.AddAsync(new RefreshToken
            {
                UserId      = user.Id,
                Token       = rtHash,
                JwtId       = jti,
                ExpiresAt   = expiresAt,
                RememberMe  = command.PostModel.RememberMe,
                CreatedByIp = ip
            }, ct);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _refreshTokenServices.SaveChangesAsync(ct);

        _logger.LogInformation("Login success for {UserId} ({Role})", user.Id, user.Role);

        return new AuthResponseDto(AccessToken: accessToken, RefreshToken: rtPlaintext, RememberMe: command.PostModel.RememberMe);
    }
}
