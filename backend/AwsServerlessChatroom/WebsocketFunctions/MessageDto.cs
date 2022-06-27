using AwsServerlessChatroom.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom.WebsocketFunctions;
public record MessageDto(Guid ChannelId, long Sequence, string Content, string FromConnection, DateTimeOffset Time)
{
    public static MessageDto FromMessage(Message message)
    {
        return new MessageDto(message.ChannelId, message.Sequence, message.Content, message.FromConnection, message.Time);
    }
}

