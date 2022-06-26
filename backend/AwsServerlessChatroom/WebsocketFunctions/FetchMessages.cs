using Amazon.Lambda.Core;
using AwsServerlessChatroom.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace AwsServerlessChatroom.WebsocketFunctions;
public class FetchMessages : RequestResponseFunctionBase<FetchMessagesRequest, FetchMessagesResponse>
{
    protected override async Task<FetchMessagesResponse> HandleRequestCore(FetchMessagesRequest request, string connectionId, ILambdaContext lambdaContext)
    {
        var repo = _serviceProvider.GetRequiredService<MessagesRepository>();
        var messages = await repo.FetchMessages(request.ChannelId, request.TakeLast, request.MaxSequence);
        return new FetchMessagesResponse(messages.Select(m => new MessageDto(m.Sequence, m.Content, m.FromConnection)).ToList());
    }
}


public class FetchMessagesRequest
{
    [Required]
    public Guid ChannelId { get; set; }

    [Required]
    [Range(1, 100)]
    public int TakeLast { get; set; }

    public long? MaxSequence { get; set; }
}

public record FetchMessagesResponse(List<MessageDto> Messages);

public record MessageDto(long Sequence, string Content, string FromConnection);
