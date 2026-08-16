using FluentAssertions;
using Faaz.Services.Identity.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Faaz.Services.Identity.IntegrationTests.Auth;

public class AuthEndpointsTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidStudentData_Returns201()
    {
        var payload = new
        {
            firstName = "Test",
            lastName  = "Student",
            email     = $"test-{Guid.NewGuid():N}@example.com",
            password  = "Test@Password1!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register/student", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonBody>();
        body!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var payload = new
        {
            firstName = "Dup",
            lastName  = "User",
            email     = email,
            password  = "Test@Password1!"
        };

        await _client.PostAsJsonAsync("/api/v1/auth/register/student", payload);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register/student", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithUnverifiedEmail_Returns403()
    {
        var email = $"unverified-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register/student", new
        {
            firstName = "Un",
            lastName  = "Verified",
            email     = email,
            password  = "Test@Password1!"
        });

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email    = email,
            password = "Test@Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ForgotPassword_WithNonexistentEmail_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new
        {
            email = "nonexistent@nowhere.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record JsonBody(bool Success, int StatusCode, string? Message, object? Data);
}
