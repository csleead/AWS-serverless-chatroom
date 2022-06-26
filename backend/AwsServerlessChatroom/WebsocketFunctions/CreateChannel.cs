using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace AwsServerlessChatroom.WebsocketFunctions;
public class CreateChannel : RequestResponseFunctionBase<CreateChannelRequest, CreateChannelResponse>
{
    protected override async Task<CreateChannelResponse> HandleRequestCore(CreateChannelRequest request, string connectionId, ILambdaContext lambdaContext)
    {
        var useCase = _serviceProvider.GetRequiredService<UseCases.CreateChannel>();
        var channelId = await useCase.Execute(request.ChannelName);
        return new CreateChannelResponse(channelId);
    }
}

public class CreateChannelRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string ChannelName { get; set; } = default!;
}

public record CreateChannelResponse(Guid ChannelId);
