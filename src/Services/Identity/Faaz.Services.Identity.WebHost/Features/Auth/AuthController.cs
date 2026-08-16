using Faaz.Services.Identity.WebHost.Features.Auth.Commands;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.Services.Identity.WebHost.Features.Auth.Queries;
using Faaz.Services.Identity.WebHost.HttpClients;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Faaz.Services.Identity.WebHost.Features.Auth;

/// <summary>Authentication endpoints — register, login, token management.</summary>
[Route("api/v1/auth")]
[Tags("Auth")]
public class AuthController : FaazApiController
{
    private readonly IMediator _mediator;
    private readonly IConsultantServiceClient _consultantClient;

    public AuthController(IMediator mediator, IConsultantServiceClient consultantClient)
    {
        _mediator         = mediator;
        _consultantClient = consultantClient;
    }

    /// <summary>Register a new student account.</summary>
    [HttpPost("register/student")]
    [EnableRateLimiting("auth")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentDto postModel, CancellationToken ct)
    {
        var userId = await _mediator.Send(new RegisterStudentCommand { PostModel = postModel }, ct);
        return StatusCode(201, ApiResponse.Created(new { userId }, "Student registered successfully."));
    }

    /// <summary>Submit a consultant Expression of Interest. Documents are optional (PDF, Word, PNG, JPG — max 10 MB each).</summary>
    [HttpPost("register/consultant")]
    [EnableRateLimiting("auth")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterConsultant([FromForm] RegisterConsultantDto postModel, CancellationToken ct)
    {
        var applicationId = await _mediator.Send(new RegisterConsultantCommand { PostModel = postModel }, ct);
        return StatusCode(201, ApiResponse.Created(new { applicationId }, "Application submitted successfully."));
    }

    /// <summary>Look up the email address associated with a consultant invite token. Used to pre-fill the account-setup form.</summary>
    [HttpGet("invite-info")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInviteInfo([FromQuery] string token, CancellationToken ct)
    {
        try
        {
            var (email, _) = await _consultantClient.ValidateInviteTokenAsync(token, ct);
            return Ok(ApiResponse.Ok(new { email }, "Invite token valid."));
        }
        catch
        {
            return BadRequest(ApiResponse.Fail(400, "Invalid or expired invite token."));
        }
    }

    /// <summary>Create consultant account after clicking invite link. Email is pre-filled from the invite and must match.</summary>
    [HttpPost("create-consultant-account")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateConsultantAccount([FromBody] CreateConsultantAccountDto postModel, CancellationToken ct)
    {
        var userId = await _mediator.Send(new CreateConsultantAccountCommand { PostModel = postModel }, ct);
        return StatusCode(201, ApiResponse.Created(new { userId }, "Consultant account created successfully."));
    }

    /// <summary>Log in. Returns an access token (JWT). The refresh token is set as an httpOnly cookie (faaz_rt).</summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginDto postModel, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand
        {
            PostModel = postModel,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RememberMe);
        return Ok(ApiResponse.Ok(new { result.AccessToken }, "Login successful."));
    }

    /// <summary>Exchange the faaz_rt httpOnly cookie for a new access token. A new faaz_rt cookie is issued in the response.</summary>
    [HttpPost("refresh-token")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        var token = Request.Cookies["faaz_rt"];
        if (string.IsNullOrEmpty(token))
            return Unauthorized(ApiResponse.Fail(401, "Refresh token missing."));

        var result = await _mediator.Send(new RefreshTokenCommand
        {
            RefreshToken = token,
            IpAddress    = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RememberMe);
        return Ok(ApiResponse.Ok(new { result.AccessToken }, "Token refreshed."));
    }

    /// <summary>Revoke the refresh token and clear the faaz_rt cookie.</summary>
    [HttpPost("logout")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        // Best-effort DB revocation — only possible when a valid bearer token is present.
        var userIdStr = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            try
            {
                await _mediator.Send(new LogoutCommand
                {
                    UserId    = userId,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                }, ct);
            }
            catch { /* ignore — cookie is cleared regardless */ }
        }
        ClearRefreshTokenCookie();
        return Ok(ApiResponse.NoContent("Logged out successfully."));
    }

    /// <summary>
    /// When rememberMe is true the cookie is persistent (survives browser restart, 30 days).
    /// When false, Expires is left null so the browser treats it as a session cookie — cleared
    /// on browser close — while the refresh token itself still caps out at 1 day server-side.
    /// </summary>
    private void SetRefreshTokenCookie(string token, bool rememberMe)
    {
        Response.Cookies.Append("faaz_rt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure   = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires  = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null,
            Path     = "/",
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Append("faaz_rt", "", new CookieOptions
        {
            HttpOnly = true,
            Secure   = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires  = DateTimeOffset.UnixEpoch,
            Path     = "/",
        });
    }

    /// <summary>Verify email address using the token from the verification email.</summary>
    [HttpPost("verify-email")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto postModel, CancellationToken ct)
    {
        await _mediator.Send(new VerifyEmailCommand { PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("Email verified successfully."));
    }

    /// <summary>Resend verification email.</summary>
    [HttpPost("resend-verification")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto postModel, CancellationToken ct)
    {
        await _mediator.Send(new ResendVerificationCommand { PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("If the email exists and is unverified, a new verification link has been sent."));
    }

    /// <summary>Request a password reset email. Token expires in 1 hour.</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto postModel, CancellationToken ct)
    {
        await _mediator.Send(new ForgotPasswordCommand { PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("If the email is registered, a reset link has been sent."));
    }

    /// <summary>Reset password using token from email.</summary>
    [HttpPost("reset-password")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto postModel, CancellationToken ct)
    {
        await _mediator.Send(new ResetPasswordCommand { PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("Password reset successfully."));
    }

    /// <summary>Get the current authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await _mediator.Send(new GetCurrentUserQuery(userId), ct);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Change the current authenticated user's password.</summary>
    [HttpPut("password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto postModel, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        await _mediator.Send(new ChangePasswordCommand { UserId = userId, PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("Password changed successfully."));
    }

    /// <summary>Permanently delete (soft-delete + PII scrub) the current authenticated user's account.</summary>
    [HttpDelete("account")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        await _mediator.Send(new DeleteAccountCommand { UserId = userId }, ct);
        return Ok(ApiResponse.NoContent("Account deleted."));
    }

    /// <summary>RS256 public key in JWKS format — used by other services to validate JWTs.</summary>
    [HttpGet(".well-known/jwks.json")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJwks(CancellationToken ct)
    {
        var jwks = await _mediator.Send(new GetJwksQuery(), ct);
        return Content(jwks, "application/json");
    }
}
