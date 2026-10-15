using System.Net;
using FluentAssertions;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace LandlordApp.Tests.Services;

/// <summary>No network: Nominatim is replaced by a stub HTTP handler.</summary>
public class NominatimGeocodingServiceTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(body) };

    private const string Valjevska =
        "[{\"lat\":\"44.8040821\",\"lon\":\"20.4651234\",\"display_name\":\"Valjevska, Beograd\"}]";

    private static (NominatimGeocodingService Service, StubHandler Handler) Create(
        Func<HttpRequestMessage, HttpResponseMessage> respond,
        Dictionary<string, string?>? config = null)
    {
        var handler = new StubHandler(respond);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(NominatimGeocodingService.HttpClientName))
            .Returns(() => new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.test/") });

        var settings = new Dictionary<string, string?> { ["Geocoding:MinIntervalMs"] = "0" };
        if (config is not null)
            foreach (var pair in config) settings[pair.Key] = pair.Value;

        var service = new NominatimGeocodingService(
            factory.Object,
            new MemoryCache(new MemoryCacheOptions()),
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
            new Mock<ILogger<NominatimGeocodingService>>().Object);

        return (service, handler);
    }

    [Fact]
    public async Task Address_is_resolved_to_coordinates_rounded_to_six_decimals()
    {
        var (service, _) = Create(_ => Json(Valjevska));

        var point = await service.GeocodeAsync("Valjevska", "Beograd", null);

        point.Should().Be(new GeoPoint(44.804082m, 20.465123m));
    }

    [Fact]
    public async Task Messy_input_is_cleaned_before_it_is_sent()
    {
        var (service, handler) = Create(_ => Json(Valjevska));

        // the exact input from the listing that had no map: "Valjevska , Beograd"
        await service.GeocodeAsync("  Valjevska , ", " Beograd ", null);

        handler.Requests.Should().ContainSingle();
        var uri = handler.Requests[0];
        uri.AbsolutePath.Should().EndWith("search");
        uri.Query.Should().Contain("q=Valjevska%2C%20Beograd").And.Contain("format=jsonv2").And.Contain("limit=1");
    }

    [Fact]
    public async Task Postal_code_is_part_of_the_query_when_present()
    {
        var (service, handler) = Create(_ => Json(Valjevska));

        await service.GeocodeAsync("Knez Mihailova 10", "Beograd", "11000");

        handler.Requests[0].Query.Should().Contain("q=Knez%20Mihailova%2010%2C%2011000%20Beograd");
    }

    [Theory]
    [InlineData(null, "Beograd")]
    [InlineData("", "Beograd")]
    [InlineData("   ", "Beograd")]
    [InlineData("Valjevska", null)]
    [InlineData("Valjevska", "  ")]
    public async Task Without_a_street_and_a_city_nothing_is_requested(string? address, string? city)
    {
        var (service, handler) = Create(_ => Json(Valjevska));

        var point = await service.GeocodeAsync(address, city, null);

        point.Should().BeNull();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Unknown_address_returns_null()
    {
        var (service, _) = Create(_ => Json("[]"));

        (await service.GeocodeAsync("Nepostojeca 1", "Nigde", null)).Should().BeNull();
    }

    [Fact]
    public async Task A_found_address_is_looked_up_only_once()
    {
        var (service, handler) = Create(_ => Json(Valjevska));

        var first = await service.GeocodeAsync("Valjevska", "Beograd", null);
        var second = await service.GeocodeAsync("VALJEVSKA", "beograd", null); // same place, different case

        second.Should().Be(first);
        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task An_unknown_address_is_not_asked_again_right_away()
    {
        var (service, handler) = Create(_ => Json("[]"));

        await service.GeocodeAsync("Nepostojeca 1", "Nigde", null);
        await service.GeocodeAsync("Nepostojeca 1", "Nigde", null);

        handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task A_failing_service_returns_null_and_is_retried_next_time()
    {
        var calls = 0;
        var (service, handler) = Create(_ =>
            ++calls == 1 ? Json("oops", HttpStatusCode.ServiceUnavailable) : Json(Valjevska));

        var first = await service.GeocodeAsync("Valjevska", "Beograd", null);
        var second = await service.GeocodeAsync("Valjevska", "Beograd", null);

        first.Should().BeNull("a 503 must not break saving a listing");
        second.Should().NotBeNull("failures are not cached");
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Garbage_response_returns_null_instead_of_throwing()
    {
        var (service, _) = Create(_ => Json("<html>not json</html>"));

        (await service.GeocodeAsync("Valjevska", "Beograd", null)).Should().BeNull();
    }

    [Theory]
    [InlineData("[{\"lat\":\"123.0\",\"lon\":\"20.4\"}]")]   // latitude out of range
    [InlineData("[{\"lat\":\"44.8\",\"lon\":\"abc\"}]")]     // not a number
    [InlineData("[{\"name\":\"no coordinates\"}]")]          // missing fields
    public async Task Implausible_coordinates_are_rejected(string body)
    {
        var (service, _) = Create(_ => Json(body));

        (await service.GeocodeAsync("Valjevska", "Beograd", null)).Should().BeNull();
    }

    [Fact]
    public async Task Geocoding_can_be_switched_off()
    {
        var (service, handler) = Create(_ => Json(Valjevska), new() { ["Geocoding:Enabled"] = "false" });

        (await service.GeocodeAsync("Valjevska", "Beograd", null)).Should().BeNull();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Country_codes_narrow_the_search_when_configured()
    {
        var (service, handler) = Create(_ => Json(Valjevska), new() { ["Geocoding:CountryCodes"] = "rs, ba" });

        await service.GeocodeAsync("Valjevska", "Beograd", null);

        handler.Requests[0].Query.Should().Contain("countrycodes=rs%2Cba");
    }

    [Fact]
    public async Task Requests_are_spaced_out_to_respect_the_one_per_second_policy()
    {
        var (service, handler) = Create(_ => Json(Valjevska), new() { ["Geocoding:MinIntervalMs"] = "300" });

        await service.GeocodeAsync("Prva ulica 1", "Beograd", null);
        var started = DateTime.UtcNow;
        await service.GeocodeAsync("Druga ulica 2", "Beograd", null);

        (DateTime.UtcNow - started).Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(250));
        handler.Requests.Should().HaveCount(2);
    }
}
