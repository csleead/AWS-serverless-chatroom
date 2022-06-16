using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Xunit;

namespace AwsServerlessChatroom.Tests;
public class ChannelRepositoryTest : IClassFixture<LocalDynamoDbFixture>, IAsyncLifetime
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    private readonly ChannelRepository _respository;

    public ChannelRepositoryTest(LocalDynamoDbFixture localDynamoDbFixture)
    {
        _dynamoDbClient = localDynamoDbFixture.DynamoDbClient;
        _respository = new ChannelRepository(_dynamoDbClient);
    }

    public async Task InitializeAsync()
    {
        _ = await _dynamoDbClient.CreateTableAsync(new CreateTableRequest
        {
            TableName = DynamoDbTableNames.Channels,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement("ChannelId", KeyType.HASH),
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition("ChannelId", ScalarAttributeType.S),
            },
            ProvisionedThroughput = new ProvisionedThroughput(5, 5),
        });
    }

    public async Task DisposeAsync()
    {
        _ = await _dynamoDbClient.DeleteTableAsync(new DeleteTableRequest
        {
            TableName = DynamoDbTableNames.Channels,
        });
    }

    [Fact]
    public async Task CanCreateChannels()
    {
        var id1 = await _respository.CreateChannel("Channel1");
        var id2 = await _respository.CreateChannel("Channel2");

        var channels = await _respository.ListChannels();
        _ = channels.Should().BeEquivalentTo(new[]
        {
            new Channel(id1, "Channel1"),
            new Channel(id2, "Channel2"),
        });
    }

    [Fact]
    public async Task ExistsChannelWorksCorrectly_HasChannel()
    {
        var id = await _respository.CreateChannel("Channel1");
        _ = (await _respository.ExistsChannel(id)).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsChannelWorksCorrectly_NoSuchChannel()
    {
        _ = (await _respository.ExistsChannel(Guid.NewGuid())).Should().BeFalse();
    }
}
