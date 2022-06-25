using Amazon.ApiGatewayManagementApi;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AwsServerlessChatroom.DataAccess;
using AwsServerlessChatroom.UseCases;
using AwsServerlessChatroom.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;
using System.Text.Json;

namespace AwsServerlessChatroom.WebsocketFunctions;
public abstract class RequestResponseFunctionBase<TRequest, TResponse>
{
    private static readonly APIGatewayProxyResponse SuccessResponse = new()
    {
        StatusCode = 200,
    };

    private static readonly JsonSchemaGeneratorSettings JsonSchemaGeneratorSettings = new()
    {
        AlwaysAllowAdditionalObjectProperties = true,
        SerializerSettings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
        }
    };

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
    };

    protected readonly ServiceProvider _serviceProvider;

    protected RequestResponseFunctionBase()
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
        _ = serviceCollection.AddSingleton<UseCases.JoinChannel>();
        _ = serviceCollection.AddSingleton<CreateChannel>();
        _ = serviceCollection.AddSingleton<SendMessage>();
        _ = serviceCollection.AddSingleton<BroadcastNewMessages>();
        _ = serviceCollection.AddSingleton<WebsocketPusher>();
        _ = serviceCollection.AddSingleton<DisconnectionCleanup>();
        _ = serviceCollection.AddSingleton<LeaveChannel>();

        _serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest apiGatewayProxyRequest, ILambdaContext lambdaContext)
    {
        var wsPusher = _serviceProvider.GetRequiredService<WebsocketPusher>();

        var message = JsonDocument.Parse(apiGatewayProxyRequest.Body);
        if (!message.RootElement.TryGetStringProperty("messageId", out var messageId))
        {
            await wsPusher.PushData(apiGatewayProxyRequest.GetConnectionId(), new Response
            {
                Message = "Requests must contain a string 'messageId'",
            });
            return SuccessResponse;
        }

        var schema = JsonSchema.FromType<TRequest>(JsonSchemaGeneratorSettings);
        var errors = schema.Validate(apiGatewayProxyRequest.Body);
        if (errors.Any())
        {
            await wsPusher.PushData(apiGatewayProxyRequest.GetConnectionId(), new Response
            {
                MessageId = messageId,
                Message = "The request is invalid for this action",
                Data = new
                {
                    Errors = errors.GroupBy(e => e.Path).ToDictionary(e => e.First().Path, e => e.Select(e => e.Kind.ToString())),
                    Schema = JsonDocument.Parse(schema.ToJson()),
                },
            });
            return SuccessResponse;
        }

        var request = System.Text.Json.JsonSerializer.Deserialize<TRequest>(apiGatewayProxyRequest.Body, JsonSerializerOptions);
        var response = await HandleRequestCore(request!, apiGatewayProxyRequest.GetConnectionId(), lambdaContext);
        await wsPusher.PushData(apiGatewayProxyRequest.GetConnectionId(), new Response
        {
            MessageId = messageId,
            Data = response,
        });

        return SuccessResponse;
    }

    protected abstract Task<TResponse> HandleRequestCore(TRequest request, string connectionId, ILambdaContext lambdaContext);
}

public class Response
{
    public string? MessageId { get; init; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}
