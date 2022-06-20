using AwsServerlessChatroom.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom.UseCases;
public class DisconnectionCleanup
{
    private readonly ChannelSubscriptionsRepository _channelSubscriptionsRepository;

    public DisconnectionCleanup(ChannelSubscriptionsRepository channelSubscriptionsRepository)
    {
        _channelSubscriptionsRepository = channelSubscriptionsRepository;
    }

    public async Task Execute(string connectionId)
    {
        var subscribedChannelIds = await _channelSubscriptionsRepository.ListChannelSubscriptionsOfConnection(connectionId);
        foreach (var channelId in subscribedChannelIds)
        {
            await _channelSubscriptionsRepository.RemoveChannelSubscription(channelId, connectionId);
        }
    }
}
