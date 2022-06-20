using AwsServerlessChatroom.DataAccess;

namespace AwsServerlessChatroom.UseCases;
public class LeaveChannel
{
    private readonly ChannelSubscriptionsRepository _channelSubscriptionsRepository;

    public LeaveChannel(ChannelSubscriptionsRepository channelSubscriptionsRepository)
    {
        _channelSubscriptionsRepository = channelSubscriptionsRepository;
    }

    public async Task Execute(string connectionId, Guid channelId)
    {
        await _channelSubscriptionsRepository.RemoveChannelSubscription(channelId, connectionId);
    }
}
