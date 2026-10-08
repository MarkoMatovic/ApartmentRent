namespace Lander.src.Modules.Payments.Paddle;

/// <summary>Raised when a Paddle Billing API call fails. Surfaced to the client as a 502/503.</summary>
public sealed class PaddleApiException : Exception
{
    public PaddleApiException(string message) : base(message) { }
}
