using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom.DataAccess;
public class MessagesRepository
{
    private static readonly Random Random = new();

    private static readonly AsyncRetryPolicy RetryPolicy = Policy
        .Handle<TransactionCanceledException>(e => e.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        .WaitAndRetryForeverAsync(_ => TimeSpan.FromMilliseconds(Random.NextInt64(1000)));

    private readonly AmazonDynamoDBClient _dbClient;


    public MessagesRepository(AmazonDynamoDBClient dbClient)
    {
        _dbClient = dbClient;
    }

    public async Task<IReadOnlyList<Message>> FetchMessages(Guid channelId, int takeLast, long? maxSequence)
    {
        maxSequence ??= await NextMessageSequence(channelId) - 1;

        var response = await _dbClient.QueryAsync(new QueryRequest
        {
            TableName = DynamoDbTableNames.Messages,
            KeyConditionExpression = "ChannelId = :channelId AND MsgSeq BETWEEN :minSequence AND :maxSequence",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":channelId", new AttributeValue(channelId.ToString()) },
                { ":minSequence", new AttributeValue() { N = $"{maxSequence - takeLast + 1}" } },
                { ":maxSequence", new AttributeValue()  { N = $"{maxSequence}" } },
            }
        });

        AwsServiceException.ThrowIfFailed(response);

        var messages = new List<Message>(response.Count);
        foreach (var item in response.Items)
        {
            messages.Add(new Message(
                Guid.Parse(item["ChannelId"].S),
                long.Parse(item["MsgSeq"].N),
                item["Content"].S,
                item["FromConnection"].S,
                DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(item["Timestamp"].N)))
            );
        }
        return messages;
    }

    public async Task<long> InsertMessage(string fromConnection, Guid channelId, string content)
    {
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            var sequence = await NextMessageSequence(channelId);
            await PutMessage(fromConnection, channelId, content, sequence);
            return sequence;
        });
    }

    private async Task<long> NextMessageSequence(Guid channelId)
    {
        var response = await _dbClient.QueryAsync(new QueryRequest
        {
            TableName = DynamoDbTableNames.MessageSequence,
            KeyConditionExpression = "ChannelId = :channelId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":channelId", new AttributeValue(channelId.ToString()) },
            },
            ProjectionExpression = "MsgSeq"
        });

        AwsServiceException.ThrowIfFailed(response);

        return long.Parse(response.Items.Single()["MsgSeq"].N) + 1;
    }

    private async Task PutMessage(string fromConnection, Guid channelId, string content, long sequence)
    {
        var tran = new TransactWriteItemsRequest();
        tran.TransactItems.Add(new TransactWriteItem
        {
            Put = new Put
            {
                TableName = DynamoDbTableNames.Messages,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "ChannelId", new AttributeValue(channelId.ToString()) },
                    { "MsgSeq", new AttributeValue { N = $"{sequence}" } },
                    { "Timestamp", new AttributeValue { N = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" } },
                    { "FromConnection", new AttributeValue(fromConnection) },
                    { "Content", new AttributeValue(content) },
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
                    { "MsgSeq", new AttributeValue { N = $"{sequence}" } },
                    { "FromConnection", new AttributeValue(fromConnection) },
                    { "Content", new AttributeValue(content) },
                },
                ConditionExpression = "MsgSeq = :sequence",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":sequence", new AttributeValue { N = $"{sequence - 1}" } }
                }
            },
        });

        var response = await _dbClient.TransactWriteItemsAsync(tran);

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
                long.Parse(item["MsgSeq"].N),
                item["Content"].S,
                item["FromConnection"].S,
                DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(item["Timestamp"].N))
            ));
        }

        return result;
    }
}
