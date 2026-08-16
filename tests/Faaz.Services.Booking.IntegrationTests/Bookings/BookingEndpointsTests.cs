using FluentAssertions;
using Faaz.Services.Booking.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Faaz.Services.Booking.IntegrationTests.Bookings;

public class BookingEndpointsTests : IClassFixture<BookingWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BookingEndpointsTests(BookingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBookingById_WithoutAuth_Returns401()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/bookings/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAvailableSlots_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/bookings/available-slots?consultantProfileId=" + Guid.NewGuid());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyBookings_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/bookings/mine");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
