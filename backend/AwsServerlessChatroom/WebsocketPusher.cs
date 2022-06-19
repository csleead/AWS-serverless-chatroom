using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Lambda.APIGatewayEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AwsServerlessChatroom.Utils;

namespace AwsServerlessChatroom;
public class WebsocketPusher
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly AmazonApiGatewayManagementApiClient _apiClient;

    public WebsocketPusher(AmazonApiGatewayManagementApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task PushData(string connectionId, object data)
    {
        using var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, data, JsonSerializerOptions);
        ms.Position = 0;

        try
        {
            var response = await _apiClient.PostToConnectionAsync(new PostToConnectionRequest
            {
                ConnectionId = connectionId,
                Data = ms,
            });

            AwsServiceException.ThrowIfFailed(response, $"Failed to send data to connection '{connectionId}'");
        }
        catch (GoneException)
        {
            // Connection no longer exists, nothing we can do
        }
    }
}
