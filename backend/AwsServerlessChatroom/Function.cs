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
    private static readonly APIGatewayProxyResponse SuccessResponse = new()
    {
        StatusCode = 200,
    };

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
        return SuccessResponse;
    }

    public async Task<APIGatewayProxyResponse> OnDisconnect()
    {
        return SuccessResponse;
    }

    public async Task<APIGatewayProxyResponse> Default(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
    {
        try
        {
            var message = JsonDocument.Parse(request.Body);
            if (!message.RootElement.TryGetProperty("action", out var actionElem))
            {
                await request.PushData(new { error = "The message isn't a valid JSON" });
                return SuccessResponse;
            }

            if (actionElem.ValueKind != JsonValueKind.String)
            {
                await request.PushData(new { error = "action must be a string" });
                return SuccessResponse;
            }

            await request.PushData(new { error = "Unsupported action" });
            return SuccessResponse;
        }
        catch (JsonException e)
        {
            await request.PushData(new { error = "The message isn't a valid JSON" });
        }
        catch (Exception e)
        {
            await request.PushData(new { error = "System Error" });
            lambdaContext.Logger.LogError(e.ToString());
        }

        return SuccessResponse;
    }

    public async Task<APIGatewayProxyResponse> JoinChannel(APIGatewayProxyRequest request)
    {
        var message = JsonDocument.Parse(request.Body);
        if (!message.RootElement.TryGetProperty("channelId", out var channelId))
        {
            await request.PushData(new { error = "The message doesn't contain a channelId" });
            return SuccessResponse;
        }

        if (!channelId.TryGetGuid(out var channelGuid))
        {
            await request.PushData(new { error = "channelId must be a GUID" });
            return SuccessResponse;
        }

        using var scope = _serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<JoinChannel>();
        var result = await useCase.Execute(request.RequestContext.ConnectionId, channelGuid);

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

        return SuccessResponse;
    }

    public async Task<APIGatewayProxyResponse> CreateChannel(APIGatewayProxyRequest request)
    {
        var message = JsonDocument.Parse(request.Body);
        if (!message.RootElement.TryGetProperty("channelName", out var channelName) || string.IsNullOrWhiteSpace(channelName.GetString()))
        {
            await request.PushData(new { error = "The message doesn't contain a channelName" });
            return SuccessResponse;
        }

        using var scope = _serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<CreateChannel>();
        var id = await useCase.Execute(channelName.GetString()!);

        await request.PushData(new
        {
            message = "Channel created successfully",
            result = new
            {
                channelId = id.ToString(),
            },
        });

        return SuccessResponse;
    }
}
