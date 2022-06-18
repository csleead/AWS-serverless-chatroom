using Xunit;
using AwsServerlessChatroom.DataAccess;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;

namespace AwsServerlessChatroom.Tests;

public class MessagesRepositoryTest : IClassFixture<LocalDynamoDbFixture>, IAsyncLifetime
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    private readonly MessagesRepository _respository;

    public MessagesRepositoryTest(LocalDynamoDbFixture localDynamoDbFixture)
    {
        _dynamoDbClient = localDynamoDbFixture.DynamoDbClient;
        _respository = new MessagesRepository(_dynamoDbClient);
    }

    public async Task InitializeAsync()
    {
        _ = await _dynamoDbClient.CreateTableAsync(new CreateTableRequest
        {
            TableName = DynamoDbTableNames.Messages,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement("ChannelId", KeyType.HASH),
                new KeySchemaElement("Timestamp", KeyType.RANGE),
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition("ChannelId", ScalarAttributeType.S),
                new AttributeDefinition("Timestamp", ScalarAttributeType.N),
            },
            ProvisionedThroughput = new ProvisionedThroughput(5, 5),
        });
    }

    public async Task DisposeAsync()
    {
        _ = await _dynamoDbClient.DeleteTableAsync(new DeleteTableRequest
        {
            TableName = DynamoDbTableNames.Messages,
        });
    }

    [Fact]
    public async Task CanSendAndRecieveMessages()
    {
        var channel1 = Guid.NewGuid();
        var (conn1, content1) = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        var (conn2, content2) = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var channel2 = Guid.NewGuid();
        var (conn3, content3) = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        var (conn4, content4) = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        await _respository.InsertMessage(conn1, channel1, content1);
        await _respository.InsertMessage(conn2, channel1, content2);
        await _respository.InsertMessage(conn3, channel2, content3);
        await _respository.InsertMessage(conn4, channel2, content4);

        var channel1Messages = await _respository.GetMessages(channel1);
        _ = channel1Messages.Should().HaveCount(2);

        var channel2Messages = await _respository.GetMessages(channel2);
        _ = channel2Messages.Should().HaveCount(2);
    }
}
