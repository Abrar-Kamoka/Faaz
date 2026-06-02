using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Faaz.Services.Consultant.WebHost;

/// <summary>Base controller — declares common response types and shared security helpers.</summary>
[ApiController]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public abstract class FaazApiController : ControllerBase
{
    protected bool IsOwner(Guid userId)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return sub is not null && Guid.TryParse(sub, out var id) && id == userId;
    }

    protected bool IsOwnerOrAdmin(Guid userId)
    {
        if (IsOwner(userId)) return true;
        return User.FindFirstValue("role") == "3";
    }
}

/// <summary>Base for internal (X-Service-Key) endpoints in the Consultant service.</summary>
public abstract class ConsultantInternalController : FaazApiController
{
    private readonly IConfiguration _config;

    protected ConsultantInternalController(IConfiguration config)
    {
        _config = config;
    }

    protected bool IsInternalRequest()
    {
        var key      = HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        var expected = _config["ServiceApiKey"];
        return !string.IsNullOrWhiteSpace(key) && key == expected;
    }
}
