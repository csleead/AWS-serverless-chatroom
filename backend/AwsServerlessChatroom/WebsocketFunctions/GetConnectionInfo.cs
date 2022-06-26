using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AwsServerlessChatroom.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom.WebsocketFunctions;
public class GetConnectionInfo : RequestResponseFunctionBase<GetConnectionInfoRequest, GetConnectionInfoResponse>
{
    protected override Task<GetConnectionInfoResponse> HandleRequestCore(GetConnectionInfoRequest request, string connectionId, ILambdaContext lambdaContext) =>
        Task.FromResult(new GetConnectionInfoResponse(connectionId));
}

public class GetConnectionInfoRequest
{

}

public record GetConnectionInfoResponse(string ConnectionId);
