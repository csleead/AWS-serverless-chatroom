using Amazon.Lambda.Core;
using AwsServerlessChatroom.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom.WebsocketFunctions;
public class ListChannels : RequestResponseFunctionBase<ListChannelsRequest, ListChannelsResponse>
{
    protected override async Task<ListChannelsResponse> HandleRequestCore(ListChannelsRequest request, string connectionId, ILambdaContext lambdaContext)
    {
        var repo = _serviceProvider.GetRequiredService<ChannelRepository>();
        var channels = await repo.ListChannels();
        return new ListChannelsResponse(channels.Select(c => new ChannelDto(c.Id, c.Name)).ToList());
    }
}

public class ListChannelsRequest
{

}

public record ListChannelsResponse(List<ChannelDto> Channels);

public record ChannelDto(Guid Id, string Name);
