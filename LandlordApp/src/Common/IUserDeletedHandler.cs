namespace Lander.src.Common;
public interface IUserDeletedHandler
{
    Task HandleAsync(int userId);
}
