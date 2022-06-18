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
public static class WebsocketPusher
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task PushData(this APIGatewayProxyRequest request, object data)
    {
        using var apiClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
        {
            ServiceURL = request.GetServiceUrl(),
        });

        using var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, data, JsonSerializerOptions);
        ms.Position = 0;

        try
        {
            var response = await apiClient.PostToConnectionAsync(new PostToConnectionRequest
            {
                ConnectionId = request.RequestContext.ConnectionId,
                Data = ms,
            });

            AwsServiceException.ThrowIfFailed(response, $"Failed to send data to connection '{request.RequestContext.ConnectionId}'");
        }
        catch (GoneException)
        {
            // Connection no longer exists, nothing we can do
        }
    }
}
