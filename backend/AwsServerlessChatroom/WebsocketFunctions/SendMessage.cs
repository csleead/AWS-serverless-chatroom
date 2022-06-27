using Amazon.Lambda.Core;
using AwsServerlessChatroom.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom.WebsocketFunctions;
public class SendMessage : RequestResponseFunctionBase<SendMessageRequest, SendMessageResponse>
{
    protected override async Task<SendMessageResponse> HandleRequestCore(SendMessageRequest request, string connectionId, ILambdaContext lambdaContext)
    {
        var useCase = _serviceProvider.GetRequiredService<UseCases.SendMessage>();
        var (result, message) = await useCase.Execute(
            connectionId,
            request.ChannelId,
            request.Content
        );

        switch (result)
        {
            case UseCases.SendMessageResult.Success:
                return new SendMessageResponse("success", MessageDto.FromMessage(message!));
            case UseCases.SendMessageResult.ChannelNotFound:
                return new SendMessageResponse("channelNotFound", null);
            default:
                throw new Exception($"Unexpected SendMessageResult: ${result}");
        }
    }
}

public class SendMessageRequest
{
    [Required]
    public Guid ChannelId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Content { get; set; } = default!;
}

public record SendMessageResponse(string Result, MessageDto Message);
