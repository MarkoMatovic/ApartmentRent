namespace Lander.src.Modules.Payments.Interfaces;

public interface IMonriCallbackHandler
{
    Task HandleCallbackAsync(string json);
}
