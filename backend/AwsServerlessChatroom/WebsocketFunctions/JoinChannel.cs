using Amazon.Lambda.Core;
using AwsServerlessChatroom.UseCases;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace AwsServerlessChatroom.WebsocketFunctions;
public class JoinChannel : RequestResponseFunctionBase<JoinChannelRequest, JoinChannelResponse>
{
    protected override async Task<JoinChannelResponse> HandleRequestCore(JoinChannelRequest request, string connectionId, ILambdaContext lambdaContext)
    {
        var useCase = _serviceProvider.GetRequiredService<UseCases.JoinChannel>();
        var result = await useCase.Execute(connectionId, request.ChannelId);

        switch (result)
        {
            case JoinChannelResult.Success:
                return new JoinChannelResponse("success");
            case JoinChannelResult.ChannelNotFound:
                return new JoinChannelResponse("channelNotFound");
            default:
                throw new Exception($"Unsupported JoinChannelResult: {result}");
        }
    }
}

public class JoinChannelRequest
{
    [Required]
    public Guid ChannelId { get; set; }
}

public record JoinChannelResponse(string Result);
