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
            _context.Messages.RemoveRange(messages);

        // Group conversations: the user's own messages go and they leave every group; the others keep theirs.
        _context.ChatGroupMessages.RemoveRange(await _context.ChatGroupMessages.Where(m => m.SenderId == userId).ToListAsync());
        _context.ChatGroupMembers.RemoveRange(await _context.ChatGroupMembers.Where(m => m.UserId == userId).ToListAsync());

        if (_context.ChangeTracker.HasChanges())
            await _context.SaveEntitiesAsync();
    }
}
