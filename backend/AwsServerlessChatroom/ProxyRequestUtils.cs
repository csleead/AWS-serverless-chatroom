using Amazon.Lambda.APIGatewayEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom;
internal static class ProxyRequestUtils
{
    public static string GetConnectionUrl(this APIGatewayProxyRequest request) =>
        $"https://{request.RequestContext.DomainName}/{request.RequestContext.Stage}/@connections/{request.RequestContext.ConnectionId}";

    public static string GetServiceUrl(this APIGatewayProxyRequest request) =>
        $"https://{request.RequestContext.DomainName}/{request.RequestContext.Stage}";

    public static string GetConnectionId(this APIGatewayProxyRequest request) =>
        request.RequestContext.ConnectionId;
}
