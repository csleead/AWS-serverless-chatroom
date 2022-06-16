using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwsServerlessChatroom;

namespace AwsServerlessChatroom.DataAccess;
public static class DynamoDbTableNames
{
    public static readonly string ChannelSubscriptions = "ServerlessChatroomApi-ChannelSubscriptions";
    public static readonly string Channels = "ServerlessChatroomApi-Channels";
}
