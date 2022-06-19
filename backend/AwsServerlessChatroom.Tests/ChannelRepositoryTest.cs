using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Xunit;
using AwsServerlessChatroom.DataAccess;

namespace AwsServerlessChatroom.Tests;
public class ChannelRepositoryTest : IClassFixture<LocalDynamoDbFixture>, IAsyncLifetime
{
    private readonly LocalDynamoDbFixture _localDynamoDbFixture;
    private readonly ChannelRepository _respository;

    public ChannelRepositoryTest(LocalDynamoDbFixture localDynamoDbFixture)
    {
        _localDynamoDbFixture = localDynamoDbFixture;
        _respository = new ChannelRepository(localDynamoDbFixture.DynamoDbClient);
    }

    public async Task InitializeAsync()
    {
        await _localDynamoDbFixture.CreateTables();
    }

    public async Task DisposeAsync()
    {
        await _localDynamoDbFixture.DeleteTables();
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
