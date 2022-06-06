using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Text;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AwsServerlessChatroom;

public class Function : IDisposable
{
    private static async Task PushDataToClient(APIGatewayProxyRequest request, object data)
    {
        using var apiClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
        {
            ServiceURL = request.GetServiceUrl(),
        });

        using var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, data);
        ms.Position = 0;

        try
        {
            var response = await apiClient.PostToConnectionAsync(new PostToConnectionRequest
            {
                ConnectionId = request.RequestContext.ConnectionId,
                Data = ms,
            });

            if (!response.IsSuccess())
            {
                throw new AwsServiceException(response, $"Failed to send data to connection '{request.RequestContext.ConnectionId}'");
            }
        }
        catch (GoneException)
        {
            // Connection no longer exists, nothing we can do
        }
    }

    private async Task JoinChannel(APIGatewayProxyRequest request, string channelId)
    {
        var queryResponse = await _dynamoDbClient.QueryAsync(new QueryRequest
        {
            TableName = "ServerlessChatroomApi-Channels",
            KeyConditionExpression = "ChannelId = :channelId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":channelId"] = new AttributeValue { S = channelId },
            },
            Select = Select.COUNT,
        });

        if (!queryResponse.IsSuccess())
        {
            throw new AwsServiceException(queryResponse, "Cannot query from dynamodb");
        }

        var channelExists = queryResponse.Count > 0;
        if (!channelExists)
        {
            await PushDataToClient(request, new { result = "Channel does not exist" });
            return;
        }

        var putItemRequest = new PutItemRequest
        {
            TableName = "ServerlessChatroomApi-ChannelSubscriptions",
            Item = new Dictionary<string, AttributeValue>
            {
                ["ChannelId"] = new AttributeValue { S = channelId },
                ["ConnectionId"] = new AttributeValue { S = request.RequestContext.ConnectionId },
            },
        };

        var response = await _dynamoDbClient.PutItemAsync(putItemRequest);
        if (!response.IsSuccess())
        {
            throw new AwsServiceException(response, "Cannot put item to dynamodb");
        }

        await PushDataToClient(request, new { result = "Joining channel success" });
    }

    private async Task CreateChannel(APIGatewayProxyRequest request, string channelName)
    {
        var channelId = Guid.NewGuid().ToString();
        var putItemRequest = new PutItemRequest
        {
            TableName = "ServerlessChatroomApi-Channels",
            Item = new Dictionary<string, AttributeValue>
            {
                ["ChannelId"] = new AttributeValue { S = channelId },
                ["Name"] = new AttributeValue { S = channelName },
            },
        };

        var response = await _dynamoDbClient.PutItemAsync(putItemRequest);
        if (!response.IsSuccess())
        {
            throw new AwsServiceException(response, "Cannot put item to dynamodb");
        }

        await PushDataToClient(request, new { result = new { channelId } });
    }

    private readonly AmazonDynamoDBClient _dynamoDbClient = new();

    public async Task<APIGatewayProxyResponse> OnConnect(APIGatewayProxyRequest request)
    {
        var putItemRequest = new PutItemRequest
        {
            TableName = "ServerlessChatroomApi-ConnectionLogs",
            Item = new Dictionary<string, AttributeValue>
            {
                ["ConnectionId"] = new AttributeValue { S = request.RequestContext.ConnectionId },
                ["Timestamp"] = new AttributeValue { N = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
                ["Action"] = new AttributeValue { S = "Connect" },
            },
        };

        var response = await _dynamoDbClient.PutItemAsync(putItemRequest);
        if (!response.IsSuccess())
        {
            throw new AwsServiceException(response, "Cannot put item to dynamodb");
        }

        return new APIGatewayProxyResponse
        {
            StatusCode = 200
        };
    }

    public async Task<APIGatewayProxyResponse> OnDisconnect(APIGatewayProxyRequest request)
    {
        var putItemRequest = new PutItemRequest
        {
            TableName = "ServerlessChatroomApi-ConnectionLogs",
            Item = new Dictionary<string, AttributeValue>
            {
                ["ConnectionId"] = new AttributeValue { S = request.RequestContext.ConnectionId },
                ["Timestamp"] = new AttributeValue { N = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() },
                ["Action"] = new AttributeValue { S = "Disconnect" },
            },
        };

        var response = await _dynamoDbClient.PutItemAsync(putItemRequest);
        if (!response.IsSuccess())
        {
            throw new AwsServiceException(response, "Cannot put item to dynamodb");
        }

        return new APIGatewayProxyResponse
        {
            StatusCode = 200
        };
    }

    public async Task<APIGatewayProxyResponse> Default(APIGatewayProxyRequest request)
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
                await PushDataToClient(request, new { error = "Unsupported method" });
                break;
        }

        return new APIGatewayProxyResponse
        {
            StatusCode = 200
        };
    }

    public void Dispose()
    {
        _dynamoDbClient?.Dispose();
    }
}
