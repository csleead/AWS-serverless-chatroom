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
                await _websocketPusher.PushData(subscription, new
                {
                    Type = "newMessage",
                    message = msg,
                });
            }
        }
    }
}
