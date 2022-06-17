using AwsServerlessChatroom.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom.UseCases;
public class CreateChannel
{
    private readonly ChannelRepository _channelRepository;

    public CreateChannel(ChannelRepository channelRepository)
    {
        _channelRepository = channelRepository;
    }

    public async Task<Guid> Execute(string channelName)
    {
        return await _channelRepository.CreateChannel(channelName);
    }
}
