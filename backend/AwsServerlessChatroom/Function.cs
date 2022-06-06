using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Text;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AwsServerlessChatroom;

public class Function : IDisposable
{
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

    public async Task<APIGatewayProxyResponse> Default(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
    {
        using var apiClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
        {
            ServiceURL = request.GetServiceUrl(),
        });

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello"))
        {
            Position = 0
        };
        var postRequest = new PostToConnectionRequest
        {
            ConnectionId = request.RequestContext.ConnectionId,
            Data = ms,
        };

        try
        {
            var response = await apiClient.PostToConnectionAsync(postRequest);
            return (int)response.HttpStatusCode switch
            {
                >= 200 and < 300 => new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = $"Data sent to {request.RequestContext.ConnectionId}",
                },
                _ => throw new Exception("Cannot post"),
            };
        }
        catch (Exception e)
        {
            lambdaContext.Logger.LogError(e.ToString());
        }

        return new APIGatewayProxyResponse
        {
            StatusCode = 500,
            Body = $"OK",
        };
    }

    public void Dispose()
    {
        _dynamoDbClient?.Dispose();
    }
}
