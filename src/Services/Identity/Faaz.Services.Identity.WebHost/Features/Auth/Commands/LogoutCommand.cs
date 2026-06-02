using Faaz.Services.Identity.Infrastructure.Interfaces.Auth;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Commands;

public class LogoutCommand : IRequest
{
    public required Guid UserId { get; set; }
    public string? IpAddress { get; set; }
}

internal sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenServices _refreshTokenServices;
    private readonly IHttpContextAccessor _httpContext;

    public LogoutCommandHandler(IRefreshTokenServices refreshTokenServices, IHttpContextAccessor httpContext)
    {
        _refreshTokenServices = refreshTokenServices;
        _httpContext = httpContext;
    }

    public async Task Handle(LogoutCommand command, CancellationToken ct)
    {
        await _refreshTokenServices.RevokeAllForUserAsync(command.UserId, command.IpAddress, ct);
        await _refreshTokenServices.SaveChangesAsync(ct);

        _httpContext.HttpContext?.Response.Cookies.Delete("faaz_rt", new CookieOptions
        {
            Path = "/api/v1/auth/refresh-token"
        });
    }
}
