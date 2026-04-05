namespace Lander.src.Common;

/// <summary>
/// Implement this interface in any module that needs to clean up data when a user is deleted.
/// Registered as IEnumerable&lt;IUserDeletedHandler&gt; in DI.
/// </summary>
public interface IUserDeletedHandler
{
    Task HandleAsync(int userId);
}
