using Faaz.Services.Identity.WebHost.Features.Auth.Commands;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.Services.Identity.WebHost.Features.Auth.Queries;
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

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
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

    /// <summary>Log in. Returns an access token (JWT) and a refresh token. Decode the JWT to read claims: sub, userId, email, role, consultant_status.</summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginDto postModel, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand
        {
            PostModel = postModel,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, ct);
        return Ok(ApiResponse.Ok(result, "Login successful."));
    }

    /// <summary>Exchange a refresh token for a new access token + refresh token pair.</summary>
    [HttpPost("refresh-token")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto body, CancellationToken ct)
    {
        var result = await _mediator.Send(new RefreshTokenCommand
        {
            RefreshToken = body.RefreshToken,
            IpAddress    = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, ct);
        return Ok(ApiResponse.Ok(result, "Token refreshed."));
    }

    /// <summary>Revoke all refresh tokens for the authenticated user.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        await _mediator.Send(new LogoutCommand
        {
            UserId    = userId,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, ct);
        return Ok(ApiResponse.NoContent("Logged out successfully."));
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

    /// <summary>RS256 public key in JWKS format — used by other services to validate JWTs.</summary>
    [HttpGet(".well-known/jwks.json")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJwks(CancellationToken ct)
    {
        var jwks = await _mediator.Send(new GetJwksQuery(), ct);
        return Content(jwks, "application/json");
    }
}
