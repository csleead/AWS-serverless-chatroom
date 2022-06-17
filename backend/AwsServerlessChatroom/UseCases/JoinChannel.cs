using Amazon.Lambda.APIGatewayEvents;
using AwsServerlessChatroom.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom.UseCases;
public class JoinChannel
{
    private readonly ChannelRepository _channelRepository;
    private readonly ChannelSubscriptionsRepository _channelSubscriptionsRepository;

    public JoinChannel(ChannelRepository channelRepository, ChannelSubscriptionsRepository channelSubscriptionsRepository)
    {
        _channelRepository = channelRepository;
        _channelSubscriptionsRepository = channelSubscriptionsRepository;
    }

    public async Task<JoinChannelResult> Execute(string connectionId, Guid channelId)
    {
        var hasChannel = await _channelRepository.ExistsChannel(channelId);

        if (!hasChannel)
        {
            return JoinChannelResult.ChannelNotFound;
        }

        await _channelSubscriptionsRepository.AddChannelSubscription(channelId, connectionId);
        return JoinChannelResult.Success;
    }
}
