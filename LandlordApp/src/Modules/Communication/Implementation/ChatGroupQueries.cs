using Lander.src.Modules.Communication.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Communication.Implementation;

internal static class ChatGroupQueries
{
    /// <summary>
    /// Group messages the user has not read yet: written by somebody else, after the newest message the
    /// user has seen in that group. Shared by the group list and the inbox badge so they always agree.
    /// </summary>
    public static IQueryable<ChatGroupMessage> UnreadGroupMessages(this CommunicationsContext context, int userId) =>
        from message in context.ChatGroupMessages.AsNoTracking()
        join member in context.ChatGroupMembers.AsNoTracking() on message.GroupId equals member.GroupId
        where member.UserId == userId
              && message.SenderId != userId
              && message.GroupMessageId > member.LastReadMessageId
        select message;
}
