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
        _ = serviceCollection.AddSingleton<DisconnectionCleanup>();
        _ = serviceCollection.AddSingleton<LeaveChannel>();

        _serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
    }

    public async Task<APIGatewayProxyResponse> OnConnect()
    {
        return SuccessResponse;
    }

    public async Task<APIGatewayProxyResponse> OnDisconnect(APIGatewayProxyRequest request)
    {
        var cleanup = _serviceProvider.GetRequiredService<DisconnectionCleanup>();
        await cleanup.Execute(request.GetConnectionId());
        return SuccessResponse;
    }

    public async Task<APIGatewayProxyResponse> Default(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
    {
        var pusher = _serviceProvider.GetRequiredService<WebsocketPusher>();

        JsonDocument? message = null;
        try
        {
            message = JsonDocument.Parse(request.Body);
            _ = message.RootElement.TryGetStringProperty("messageId", out var messageId);

            if (!message.RootElement.TryGetProperty("action", out var actionElem))
            {
                await pusher.PushData(request.GetConnectionId(), new
                {
                    messageId,
                    error = "The message isn't a valid JSON"
                });
                return SuccessResponse;
            }

            if (actionElem.ValueKind != JsonValueKind.String)
            {
                await pusher.PushData(request.GetConnectionId(), new
                {
                    messageId,
                    error = "action must be a string"
                });
                return SuccessResponse;
            }

            await pusher.PushData(request.GetConnectionId(), new
            {
                error = "Unsupported action"
            });
            return SuccessResponse;
        }
        catch (JsonException e)
        {
            await pusher.PushData(request.GetConnectionId(), new
            {
                error = "The message isn't a valid JSON"
            });
        }
        catch (Exception e)
        {
            string? messageId = null;
            _ = message?.RootElement.TryGetStringProperty("messageId", out messageId);

            await pusher.PushData(request.GetConnectionId(), new
            {
                messageId,
                error = "System Error"
            });
            lambdaContext.Logger.LogError(e.ToString());
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
