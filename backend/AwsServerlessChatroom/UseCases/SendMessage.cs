using AwsServerlessChatroom.DataAccess;

namespace AwsServerlessChatroom.UseCases;
public class SendMessage
{
    private readonly ChannelRepository _channelRepository;
    private readonly MessagesRepository _messagesRepository;

    public SendMessage(ChannelRepository channelRepository, MessagesRepository messagesRepository)
    {
        _channelRepository = channelRepository;
        _messagesRepository = messagesRepository;
    }

    public async Task<(SendMessageResult, long sequence)> Execute(string fromConnection, Guid channel, string message)
    {
        if (!await _channelRepository.ExistsChannel(channel))
        {
            return (SendMessageResult.ChannelNotFound, -1);
        }

        var messageSeq = await _messagesRepository.InsertMessage(fromConnection, channel, message);
        return (SendMessageResult.Success, messageSeq);
    }
}
