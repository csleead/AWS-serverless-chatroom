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

    public async Task<(SendMessageResult, Message? message)> Execute(string fromConnection, Guid channel, string content)
    {
        if (!await _channelRepository.ExistsChannel(channel))
        {
            return (SendMessageResult.ChannelNotFound, null);
        }

        var message = await _messagesRepository.InsertMessage(fromConnection, channel, content);
        return (SendMessageResult.Success, message);
    }
}
