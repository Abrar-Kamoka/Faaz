using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using MediatR;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Queries;

public record GetJwksQuery : IRequest<string>;

internal sealed class GetJwksQueryHandler : IRequestHandler<GetJwksQuery, string>
{
    private readonly ITokenService _tokenService;

    public GetJwksQueryHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public Task<string> Handle(GetJwksQuery query, CancellationToken ct)
    {
        var result = _tokenService.GetJwksJson();
        return Task.FromResult(result);
    }
}
