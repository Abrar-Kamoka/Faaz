using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Identity.WebHost;

/// <summary>Base controller — declares common response types so endpoints don't have to.</summary>
[ApiController]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public abstract class FaazApiController : ControllerBase { }
