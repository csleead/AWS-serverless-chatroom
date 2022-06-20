using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AwsServerlessChatroom.DataAccess;
using AwsServerlessChatroom.UseCases;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using AwsServerlessChatroom.Utils;
using Amazon.Lambda.DynamoDBEvents;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AwsServerlessChatroom;

public class Function
{
    private static readonly APIGatewayProxyResponse SuccessResponse = new()
    {
        StatusCode = 200,
    };

    private readonly ServiceProvider _serviceProvider;

    public Function()
    {
        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddSingleton<AmazonDynamoDBClient>();
        _ = serviceCollection.AddSingleton(new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
        {
            ServiceURL = WebSocketOptions.ServiceUrl
        }));
        _ = serviceCollection.AddSingleton<ChannelRepository>();
        _ = serviceCollection.AddSingleton<ChannelSubscriptionsRepository>();
        _ = serviceCollection.AddSingleton<MessagesRepository>();
        _ = serviceCollection.AddSingleton<JoinChannel>();
        _ = serviceCollection.AddSingleton<CreateChannel>();
        _ = serviceCollection.AddSingleton<SendMessage>();
        _ = serviceCollection.AddSingleton<BroadcastNewMessages>();
        _ = serviceCollection.AddSingleton<WebsocketPusher>();

        _serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
    }

    public async Task<APIGatewayProxyResponse> OnConnect()
    {
        return SuccessResponse;
    }

    public async Task<APIGatewayProxyResponse> OnDisconnect()
    {
        return SuccessResponse;
    }

    public async Task<APIGatewayProxyResponse> Default(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
    {
        var pusher = _serviceProvider.GetRequiredService<WebsocketPusher>();
        try
        {
            var message = JsonDocument.Parse(request.Body);
            if (!message.RootElement.TryGetProperty("action", out var actionElem))
            {
                await pusher.PushData(request.GetConnectionId(), new { error = "The message isn't a valid JSON" });
                return SuccessResponse;
            }

            if (actionElem.ValueKind != JsonValueKind.String)
            {
                await pusher.PushData(request.GetConnectionId(), new { error = "action must be a string" });
                return SuccessResponse;
            }

            await pusher.PushData(request.GetConnectionId(), new { error = "Unsupported action" });
            return SuccessResponse;
        }
        catch (JsonException e)
        {
            await pusher.PushData(request.GetConnectionId(), new { error = "The message isn't a valid JSON" });
        }
        catch (Exception e)
        {
            await pusher.PushData(request.GetConnectionId(), new { error = "System Error" });
            lambdaContext.Logger.LogError(e.ToString());
        }

        return SuccessResponse;
    }

    public async Task<APIGatewayProxyResponse> JoinChannel(APIGatewayProxyRequest request)
    {
        var pusher = _serviceProvider.GetRequiredService<WebsocketPusher>();
        var message = JsonDocument.Parse(request.Body);
        if (!message.RootElement.TryGetProperty("channelId", out var channelId))
        {
            await pusher.PushData(request.GetConnectionId(), new { error = "The message doesn't contain a channelId" });
            return SuccessResponse;
        }

        if (!channelId.TryGetGuid(out var channelGuid))
        {
            await pusher.PushData(request.GetConnectionId(), new { error = "channelId must be a GUID" });
            return SuccessResponse;
        }

        using var scope = _serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<JoinChannel>();
        var result = await useCase.Execute(request.RequestContext.ConnectionId, channelGuid);

        switch (result)
        {
            case JoinChannelResult.Success:
                await pusher.PushData(request.GetConnectionId(), new { result = new { channelId } });
                break;
            case JoinChannelResult.ChannelNotFound:
                await pusher.PushData(request.GetConnectionId(), new { error = "Channel not found" });
                break;
            default:
                throw new Exception($"Unsupported JoinChannelResult: {result}");
        }

        return SuccessResponse;
    }

    public async Task<APIGatewayProxyResponse> CreateChannel(APIGatewayProxyRequest request)
    {
        var pusher = _serviceProvider.GetRequiredService<WebsocketPusher>();
        var message = JsonDocument.Parse(request.Body);
        if (!message.RootElement.TryGetProperty("channelName", out var channelName) || string.IsNullOrWhiteSpace(channelName.GetString()))
        {
            await pusher.PushData(request.GetConnectionId(), new { error = "The message doesn't contain a channelName" });
            return SuccessResponse;
        }

        using var scope = _serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<CreateChannel>();
        var id = await useCase.Execute(channelName.GetString()!);

        await pusher.PushData(request.GetConnectionId(), new
        {
            message = "Channel created successfully",
            result = new
            {
                channelId = id.ToString(),
            },
        });

        return SuccessResponse;
    }

    public async Task<APIGatewayProxyResponse> ListChannels(APIGatewayProxyRequest request)
    {
        var pusher = _serviceProvider.GetRequiredService<WebsocketPusher>();
        var repo = _serviceProvider.GetRequiredService<ChannelRepository>();
        var channels = await repo.ListChannels();
        await pusher.PushData(request.GetConnectionId(), channels);
        return SuccessResponse;
    }

    public async Task<APIGatewayProxyResponse> SendMessage(APIGatewayProxyRequest request)
    {
        var pusher = _serviceProvider.GetRequiredService<WebsocketPusher>();
        var body = JsonDocument.Parse(request.Body);
        if (!body.RootElement.TryGetProperty("channelId", out var channelIdELem) || !channelIdELem.TryGetGuid(out var channelId))
        {
            await pusher.PushData(request.GetConnectionId(), new { error = "The message doesn't contain a channelId or channelId is not a valid GUID" });
            return SuccessResponse;
        }

        if (!body.RootElement.TryGetStringProperty("message", out var message) || string.IsNullOrWhiteSpace(message))
        {
            await pusher.PushData(request.GetConnectionId(), new { error = "The message doesn't contain a message field or the field is empty" });
            return SuccessResponse;
        }

        using var scope = _serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<SendMessage>();
        var result = await useCase.Execute(
            request.GetConnectionId(),
            channelId,
            message
        );

        switch (result)
        {
            case SendMessageResult.Success:
                await pusher.PushData(request.GetConnectionId(), new
                {
                    message = "Message sent",
                });
                break;
            case SendMessageResult.ChannelNotFound:
                await pusher.PushData(request.GetConnectionId(), new
                {
                    error = "No such channel",
                });
                break;
        }

        return SuccessResponse;
    }

    public async Task OnNewMessages(DynamoDBEvent @event)
    {
        var messages = new List<Message>(@event.Records.Count);
        foreach (var record in @event.Records)
        {
            var image = record.Dynamodb.NewImage;
            var channelId = Guid.Parse(image["ChannelId"].S);
            var sequence = long.Parse(image["MsgSeq"].N);
            var timestamp = long.Parse(image["Timestamp"].N);
            var fromConnectionId = image["FromConnection"].S;
            var content = image["Content"].S;

            var msg = new Message(channelId, sequence, content, fromConnectionId, DateTimeOffset.FromUnixTimeMilliseconds(timestamp));
            messages.Add(msg);
        }

        var useCase = _serviceProvider.GetRequiredService<BroadcastNewMessages>();
        await useCase.Execute(messages);
    }
}
