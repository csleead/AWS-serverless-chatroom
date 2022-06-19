using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwsServerlessChatroom.DataAccess;
using AwsServerlessChatroom;

namespace AwsServerlessChatroom.DataAccess;
public class ChannelRepository : IDisposable
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;

    public ChannelRepository(AmazonDynamoDBClient dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient;
    }

    public async Task<IReadOnlyList<Channel>> ListChannels()
    {
        var response = await _dynamoDbClient.ScanAsync(new ScanRequest
        {
            TableName = DynamoDbTableNames.Channels,
        });

        AwsServiceException.ThrowIfFailed(response);

        var list = new List<Channel>(response.Count);
        foreach (var item in response.Items)
        {
            list.Add(new Channel(Guid.Parse(item["ChannelId"].S), item["Name"].S));
        }

        return list;
    }

    public async Task<Guid> CreateChannel(string channelName)
    {
        var channelId = Guid.NewGuid();

        var tran = new TransactWriteItemsRequest();

        tran.TransactItems.Add(new TransactWriteItem
        {
            Put = new Put
            {
                TableName = DynamoDbTableNames.Channels,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "ChannelId", new AttributeValue(channelId.ToString()) },
                    { "Name", new AttributeValue(channelName) },
                }
            }
        });

        tran.TransactItems.Add(new TransactWriteItem
        {
            Put = new Put
            {
                TableName = DynamoDbTableNames.MessageSequence,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "ChannelId", new AttributeValue(channelId.ToString()) },
                    { "MsgSeq", new AttributeValue { N = "-1" } },
                }
            }
        });

        var response = await _dynamoDbClient.TransactWriteItemsAsync(tran);

        AwsServiceException.ThrowIfFailed(response);

        return channelId;
    }

    public async Task<bool> ExistsChannel(Guid id)
    {
        var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
        {
            TableName = DynamoDbTableNames.Channels,
            Key = new Dictionary<string, AttributeValue>
            {
                { "ChannelId", new AttributeValue(id.ToString()) }
            },
        });

        AwsServiceException.ThrowIfFailed(response);

        return response.Item.Count > 0;
    }

    public void Dispose() => _dynamoDbClient?.Dispose();
}
