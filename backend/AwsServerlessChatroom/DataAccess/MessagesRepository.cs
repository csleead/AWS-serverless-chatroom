using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom.DataAccess;
public class MessagesRepository
{
    private readonly AmazonDynamoDBClient _dbClient;

    public MessagesRepository(AmazonDynamoDBClient dbClient)
    {
        _dbClient = dbClient;
    }

    public async Task InsertMessage(string fromConnection, Guid channelId, string content)
    {
        var response = await _dbClient.PutItemAsync(new PutItemRequest
        {
            TableName = DynamoDbTableNames.Messages,
            Item = new Dictionary<string, AttributeValue>
            {
                { "ChannelId", new AttributeValue(channelId.ToString()) },
                { "Timestamp", new AttributeValue { N = $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}" } },
                { "FromConnection", new AttributeValue(fromConnection) },
                { "Content", new AttributeValue(content) },
            }
        });

        AwsServiceException.ThrowIfFailed(response);
    }

    public async Task<IReadOnlyList<Message>> GetMessages(Guid channelId)
    {
        var response = await _dbClient.QueryAsync(new QueryRequest
        {
            TableName = DynamoDbTableNames.Messages,
            KeyConditionExpression = "ChannelId = :channelId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":channelId", new AttributeValue(channelId.ToString()) },
            },
        });

        AwsServiceException.ThrowIfFailed(response);

        var result = new List<Message>(response.Count);
        foreach (var item in response.Items)
        {
            result.Add(new Message(
                Guid.Parse(item["ChannelId"].S),
                item["Content"].S,
                item["FromConnection"].S,
                DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(item["Timestamp"].N))
            ));
        }

        return result;
    }
}
