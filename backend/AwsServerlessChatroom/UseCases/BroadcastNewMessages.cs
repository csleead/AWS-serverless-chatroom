using AwsServerlessChatroom.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom.UseCases;
public class BroadcastNewMessages
{
    private readonly ChannelSubscriptionsRepository _channelSubscriptionsRepository;
    private readonly WebsocketPusher _websocketPusher;

    public BroadcastNewMessages(ChannelSubscriptionsRepository channelSubscriptionsRepository, WebsocketPusher websocketPusher)
    {
        _channelSubscriptionsRepository = channelSubscriptionsRepository;
        _websocketPusher = websocketPusher;
    }

    public async Task Execute(IReadOnlyCollection<Message> messages)
    {
        foreach (var msg in messages)
        {
            var subscriptions = await _channelSubscriptionsRepository.GetChannelSubscriptions(msg.ChannelId);
            foreach (var subscription in subscriptions)
            {
                if (subscription == msg.FromConnection)
                {
                    continue;
                }
                await _websocketPusher.PushData(subscription, new NewMessageDto(MessageDto.FromMessage(msg)));
            }
        }
    }

    private record NewMessageDto(MessageDto Data)
    {
        public string Type { get; } = "newMessage";
    }

    private record MessageDto(Guid ChannelId, long Sequence, string Content, string FromConnection, DateTimeOffset Time)
    {
        public static MessageDto FromMessage(Message message)
        {
            return new MessageDto(message.ChannelId, message.Sequence, message.Content, message.FromConnection, message.Time);
        }
    }
}
