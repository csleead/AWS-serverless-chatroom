using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AwsServerlessChatroom;

public class Function
{
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogLine($"Input: {request.Body}");
        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = $"Hello World! {request.Body}",
            Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } },
        };
    }
}
