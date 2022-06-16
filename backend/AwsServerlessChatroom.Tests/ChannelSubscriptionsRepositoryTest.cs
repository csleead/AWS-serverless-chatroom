using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.WaitStrategies;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using AwsServerlessChatroom.DataAccess;

namespace AwsServerlessChatroom.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "False alarm for fluent assertions")]
public class ChannelSubscriptionsRepositoryTest : IClassFixture<LocalDynamoDbFixture>, IAsyncLifetime
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    private readonly ChannelSubscriptionsRepository _respository;

    public ChannelSubscriptionsRepositoryTest(LocalDynamoDbFixture localDynamoDbFixture)
    {
        _dynamoDbClient = localDynamoDbFixture.DynamoDbClient;
        _respository = new ChannelSubscriptionsRepository(_dynamoDbClient);
    }

    public async Task InitializeAsync()
    {
        _ = await _dynamoDbClient.CreateTableAsync(new CreateTableRequest
        {
            TableName = DynamoDbTableNames.ChannelSubscriptions,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement("ChannelId", KeyType.HASH),
                new KeySchemaElement("ConnectionId", KeyType.RANGE),
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition("ChannelId", ScalarAttributeType.S),
                new AttributeDefinition("ConnectionId", ScalarAttributeType.S),
            },
            ProvisionedThroughput = new ProvisionedThroughput(5, 5),
        });
    }

    public async Task DisposeAsync()
    {
        _ = await _dynamoDbClient.DeleteTableAsync(new DeleteTableRequest
        {
            TableName = DynamoDbTableNames.ChannelSubscriptions,
        });
    }

    [Fact]
    public async Task ReturnEmptySetWhenNoSubscriptions()
    {
        var channelId = Guid.NewGuid();
        var result = await _respository.GetChannelSubscriptions(channelId);
        result.Should().BeEmpty();
    }


    [Fact]
    public async Task CanAddChannelSubscription()
    {
        var channelId = Guid.NewGuid();
        var connectionId1 = Guid.NewGuid().ToString();
        var connectionId2 = Guid.NewGuid().ToString();

        await _respository.AddChannelSubscription(channelId, connectionId1);
        await _respository.AddChannelSubscription(channelId, connectionId2);

        var result = await _respository.GetChannelSubscriptions(channelId);
        result.Should().BeEquivalentTo(new[] { connectionId1, connectionId2 });
    }

    [Fact]
    public async Task NoErrorWhenAddingWithSameChannelAndConnection()
    {
        var channelId = Guid.NewGuid();
        var connectionId = Guid.NewGuid().ToString();

        await _respository.AddChannelSubscription(channelId, connectionId);
        await _respository.AddChannelSubscription(channelId, connectionId);

        var result = await _respository.GetChannelSubscriptions(channelId);
        result.Should().BeEquivalentTo(new[] { connectionId });
    }

    [Fact]
    public async Task CanRemoveSubscription()
    {
        var channelId = Guid.NewGuid();
        var connectionId = Guid.NewGuid().ToString();

        await _respository.AddChannelSubscription(channelId, connectionId);
        await _respository.RemoveChannelSubscription(channelId, connectionId);

        var result = await _respository.GetChannelSubscriptions(channelId);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task NoErrorWhenRemovingNotExistsSubscription()
    {
        var channelId = Guid.NewGuid();
        var connectionId = Guid.NewGuid().ToString();

        await _respository.RemoveChannelSubscription(channelId, connectionId);
    }
}
