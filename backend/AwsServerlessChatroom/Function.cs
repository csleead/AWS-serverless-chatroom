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

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AwsServerlessChatroom;

public class Function
{
    private readonly ServiceProvider _serviceProvider;

    public Function()
    {
        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddSingleton<AmazonDynamoDBClient>();
        _ = serviceCollection.AddSingleton<ChannelRepository>();
        _ = serviceCollection.AddSingleton<ChannelSubscriptionsRepository>();
        _ = serviceCollection.AddSingleton<JoinChannel>();
        _ = serviceCollection.AddSingleton<CreateChannel>();

        _serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
    }

    public async Task<APIGatewayProxyResponse> OnConnect()
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = 200
        };
    }

    public async Task<APIGatewayProxyResponse> OnDisconnect()
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = 200
        };
    }

    public async Task<APIGatewayProxyResponse> Default(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
    {
        try
        {
            var message = JsonDocument.Parse(request.Body);
            switch (message.RootElement.GetProperty("method").GetString())
            {
                case "createChannel":
                    await CreateChannel(request, message.RootElement.GetProperty("channelName").GetString()!);
                    break;
                case "joinChannel":
                    await JoinChannel(request, message.RootElement.GetProperty("channelId").GetString()!);
                    break;
                default:
                    await request.PushData(new { error = "Unsupported method" });
                    break;
            }
        }
        catch (Exception e)
        {
            await request.PushData(new { error = "System Error" });
            lambdaContext.Logger.LogError(e.ToString());
        }

        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
        };
    }

    private async Task JoinChannel(APIGatewayProxyRequest request, string channelId)
    {
        if (!Guid.TryParse(channelId, out var guid))
        {
            await request.PushData(new { error = "channelId must be a GUID" });
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<JoinChannel>();
        var result = await useCase.Execute(request.RequestContext.ConnectionId, guid);

        switch (result)
        {
            case JoinChannelResult.Success:
                await request.PushData(new { result = new { channelId } });
                break;
            case JoinChannelResult.ChannelNotFound:
                await request.PushData(new { error = "Channel not found" });
                break;
            default:
                throw new Exception($"Unsupported JoinChannelResult: {result}");
        }
    }

    private async Task CreateChannel(APIGatewayProxyRequest request, string channelName)
    {
        using var scope = _serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<CreateChannel>();
        var id = await useCase.Execute(channelName);

        await request.PushData(new { result = id.ToString(), message = "Channel created successfully" });
    }
}
