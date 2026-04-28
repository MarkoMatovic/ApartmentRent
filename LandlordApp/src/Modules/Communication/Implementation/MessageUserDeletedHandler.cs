using Lander.src.Common;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Communication.Implementation;

/// <summary>
/// Deletes all messages sent or received by a user when the account is deleted.
/// </summary>
public class MessageUserDeletedHandler : IUserDeletedHandler
{
    private readonly CommunicationsContext _context;

    public MessageUserDeletedHandler(CommunicationsContext context)
        => _context = context;

    public async Task HandleAsync(int userId)
    {
        var messages = await _context.Messages
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .ToListAsync();

        if (messages.Count > 0)
        {
            _context.Messages.RemoveRange(messages);
            await _context.SaveEntitiesAsync();
        }
    }
}
