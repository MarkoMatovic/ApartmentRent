using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;

namespace LandlordApp.Tests.E2eTests.Shared;

/// <summary>
/// Generic base for typed endpoint wrappers.
/// Encapsulates the URL, HTTP method and response parsing so test code stays
/// at the "business language" level and never repeats raw URL strings.
///
/// Usage:
///   var result = await new LoginEndpoint(client).CallAsync(dto);
///   result.AccessToken.Should().NotBeNullOrEmpty();
/// </summary>
public abstract class ApiEndpointBase<TRequest, TResponse>
{
    protected readonly HttpClient Client;

    protected ApiEndpointBase(HttpClient client) => Client = client;

    protected abstract string Url { get; }
    protected abstract HttpMethod Method { get; }

    // ── Core call ─────────────────────────────────────────────────────────────

    public async Task<HttpResponseMessage> CallRawAsync(TRequest dto)
    {
        var request = new HttpRequestMessage(Method, Url)
        {
            Content = JsonContent.Create(dto)
        };
        return await Client.SendAsync(request);
    }

    /// <summary>Calls the endpoint, asserts 2xx, and deserialises the body as <typeparamref name="TResponse"/>.</summary>
    public async Task<TResponse> CallAsync(TRequest dto)
    {
        var response = await CallRawAsync(dto);
        await HttpResponseMessageAssertionsExtensions.HaveSuccessStatusCodeAsync(response);
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    /// <summary>
    /// Calls the endpoint and returns the raw JSON body as a <see cref="JsonElement"/>
    /// (useful when the response schema varies or you only need a single field).
    /// </summary>
    public async Task<JsonElement> CallForJsonAsync(TRequest dto)
    {
        var response = await CallRawAsync(dto);
        await HttpResponseMessageAssertionsExtensions.HaveSuccessStatusCodeAsync(response);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}

// ── FluentAssertions helpers ─────────────────────────────────────────────────

public static class HttpResponseMessageAssertionsExtensions
{
    /// <summary>Async-friendly assertion: asserts the response has a 2xx status code and
    /// includes the response body in the failure message.</summary>
    public static async Task HaveSuccessStatusCodeAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var _ = new AssertionScope();
        ((int)response.StatusCode).Should().BeInRange(200, 299,
            because: $"expected success but got {(int)response.StatusCode} with body: {body}");
    }

    public static async Task HaveStatusCodeAsync(this HttpResponseMessage response, System.Net.HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expected,
            because: $"body was: {body}");
    }
}
