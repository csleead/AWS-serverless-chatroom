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

    public BroadcastNewMessages(ChannelSubscriptionsRepository channelSubscriptionsRepository)
    {
        _channelSubscriptionsRepository = channelSubscriptionsRepository;
    }

    public async Task Execute(IReadOnlyCollection<Message> messages)
    {
        foreach (var msg in messages)
        {
            var subscriptions = await _channelSubscriptionsRepository.GetChannelSubscriptions(msg.ChannelId);
        }
    }
}
