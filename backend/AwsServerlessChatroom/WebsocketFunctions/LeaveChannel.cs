using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
namespace AwsServerlessChatroom.WebsocketFunctions;
public class LeaveChannel : RequestResponseFunctionBase<LeaveChannelRequest, LeaveChannelResponse>
{
    protected override async Task<LeaveChannelResponse> HandleRequestCore(LeaveChannelRequest request, string connectionId, ILambdaContext lambdaContext)
    {
        var leaveChannel = _serviceProvider.GetRequiredService<UseCases.LeaveChannel>();
        await leaveChannel.Execute(connectionId, request.ChannelId);
        return new LeaveChannelResponse("success");
    }
}


public class LeaveChannelRequest
{
    [Required]
    public Guid ChannelId { get; set; }
}

public record LeaveChannelResponse(string Result);
