using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Auth;
using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
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
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenServices _refreshTokenServices;
    private readonly IConsultantServiceClient _consultantClient;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IRefreshTokenServices refreshTokenServices,
        IConsultantServiceClient consultantClient,
        IHttpContextAccessor httpContext,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenServices = refreshTokenServices;
        _consultantClient = consultantClient;
        _httpContext = httpContext;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(command.PostModel.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, command.PostModel.Password))
        {
            _logger.LogWarning("Login failed for {Email}", command.PostModel.Email);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

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

        var accessToken = _tokenService.GenerateAccessToken(user, out var jti);
        var (rtPlaintext, rtHash) = _tokenService.GenerateRefreshToken();
        var ip = command.IpAddress ?? _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var existingToken = await _refreshTokenServices.GetByUserIdAsync(user.Id, ct);
        if (existingToken is not null)
        {
            existingToken.Token          = rtHash;
            existingToken.JwtId          = jti;
            existingToken.ExpiresAt      = DateTime.UtcNow.AddDays(7);
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
                ExpiresAt   = DateTime.UtcNow.AddDays(7),
                CreatedByIp = ip
            }, ct);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _refreshTokenServices.SaveChangesAsync(ct);

        _logger.LogInformation("Login success for {UserId} ({Role})", user.Id, user.Role);

        return new AuthResponseDto(AccessToken: accessToken, RefreshToken: rtPlaintext);
    }
}
