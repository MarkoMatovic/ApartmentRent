using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Lander.src.Modules.Payments.Paddle;
using Xunit;

namespace LandlordApp.Tests.Services;

/// <summary>
/// The signature verifier is the only thing standing between a forged HTTP POST and
/// granting tokens/credits for free, so it gets exhaustive coverage: a valid signature
/// passes, and every way of tampering (body, timestamp, secret, header shape) fails.
/// </summary>
public class PaddleSignatureVerifierTests
{
    private const string Secret = "pdl_ntfset_test_secret_value_1234567890";
    private readonly PaddleSignatureVerifier _verifier = new();

    private static string Sign(string ts, string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{ts}:{body}"));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Header(string ts, string h1) => $"ts={ts};h1={h1}";

    private static string Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    [Fact]
    public void Verify_ValidSignature_ReturnsTrue()
    {
        var ts = Now();
        var body = """{"event_type":"transaction.completed","data":{"id":"txn_1"}}""";
        var header = Header(ts, Sign(ts, body, Secret));

        _verifier.Verify(header, body, Secret, maxAgeSeconds: 60).Should().BeTrue();
    }

    [Fact]
    public void Verify_TamperedBody_ReturnsFalse()
    {
        var ts = Now();
        var body = """{"event_type":"transaction.completed","data":{"id":"txn_1"}}""";
        var header = Header(ts, Sign(ts, body, Secret));

        // Attacker swaps the payload after the signature was computed.
        var tampered = body.Replace("txn_1", "txn_HACKED");
        _verifier.Verify(header, tampered, Secret, maxAgeSeconds: 60).Should().BeFalse();
    }

    [Fact]
    public void Verify_WrongSecret_ReturnsFalse()
    {
        var ts = Now();
        var body = """{"a":1}""";
        var header = Header(ts, Sign(ts, body, "the-wrong-secret"));

        _verifier.Verify(header, body, Secret, maxAgeSeconds: 60).Should().BeFalse();
    }

    [Fact]
    public void Verify_StaleTimestamp_ReturnsFalse()
    {
        var oldTs = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600).ToString(); // 1h old
        var body = """{"a":1}""";
        var header = Header(oldTs, Sign(oldTs, body, Secret));

        // Signature itself is valid, but the timestamp is far outside the replay window.
        _verifier.Verify(header, body, Secret, maxAgeSeconds: 5).Should().BeFalse();
    }

    [Fact]
    public void Verify_StaleTimestamp_PassesWhenReplayCheckDisabled()
    {
        var oldTs = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600).ToString();
        var body = """{"a":1}""";
        var header = Header(oldTs, Sign(oldTs, body, Secret));

        _verifier.Verify(header, body, Secret, maxAgeSeconds: 0).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("h1=abc")]           // no ts
    [InlineData("ts=123")]           // no h1
    public void Verify_MalformedHeader_ReturnsFalse(string? header)
    {
        _verifier.Verify(header, """{"a":1}""", Secret, maxAgeSeconds: 0).Should().BeFalse();
    }

    [Fact]
    public void Verify_EmptySecret_ReturnsFalse()
    {
        var ts = Now();
        var body = """{"a":1}""";
        var header = Header(ts, Sign(ts, body, Secret));

        _verifier.Verify(header, body, string.Empty, maxAgeSeconds: 0).Should().BeFalse();
    }
}
