using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace AwsServerlessChatroom;
public class ChannelSubscriptionsRepository
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;

    public ChannelSubscriptionsRepository(AmazonDynamoDBClient dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient;
    }

    public async Task<ISet<string>> GetChannelSubscriptions(Guid channelId)
    {
        var response = await _dynamoDbClient.QueryAsync(new QueryRequest
        {
            TableName = DynamoDbTableNames.ChannelSubscriptions,
            KeyConditionExpression = "ChannelId = :channelId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":channelId", new AttributeValue(channelId.ToString()) },
                },
            ProjectionExpression = "ConnectionId",
        });

        AwsServiceException.ThrowIfFailed(response);

        var set = new HashSet<string>(response.Count);
        foreach (var connectionId in response.Items.Select(x => x["ConnectionId"].S))
        {
            _ = set.Add(connectionId);
        }

        return set;
    }

    public async Task AddChannelSubscription(Guid channelId, string connectionId)
    {
        var response = await _dynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = DynamoDbTableNames.ChannelSubscriptions,
            Item = new Dictionary<string, AttributeValue>
            {
                { "ChannelId", new AttributeValue(channelId.ToString()) },
                { "ConnectionId", new AttributeValue(connectionId) },
            }
        });

        AwsServiceException.ThrowIfFailed(response);
    }

    public async Task RemoveChannelSubscription(Guid channelId, string connectionId)
    {
        var response = await _dynamoDbClient.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = DynamoDbTableNames.ChannelSubscriptions,
            Key = new Dictionary<string, AttributeValue>
            {
                { "ChannelId", new AttributeValue(channelId.ToString()) },
                { "ConnectionId", new AttributeValue(connectionId) },
            },
        });

        AwsServiceException.ThrowIfFailed(response);
    }
}
