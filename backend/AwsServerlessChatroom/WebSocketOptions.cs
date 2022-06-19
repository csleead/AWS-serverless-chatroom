using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom;
public static class WebSocketOptions
{
    private static readonly string WebSocketStageUrl = Environment.GetEnvironmentVariable("WEBSOCKET_STAGE_URL")?.Trim() ?? throw new Exception("WEBSOCKET_STAGE_URL is required");
    public static readonly string ServiceUrl = WebSocketStageUrl.Replace("wss://", "https://");
}
