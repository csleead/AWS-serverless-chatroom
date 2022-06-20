using Xunit;
using AwsServerlessChatroom.DataAccess;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;

namespace AwsServerlessChatroom.Tests;

public class MessagesRepositoryTest : IClassFixture<LocalDynamoDbFixture>, IAsyncLifetime
{
    private readonly LocalDynamoDbFixture _localDynamoDbFixture;
    private readonly MessagesRepository _respository;
    private readonly ChannelRepository _channelRespository;

    public MessagesRepositoryTest(LocalDynamoDbFixture localDynamoDbFixture)
    {
        _localDynamoDbFixture = localDynamoDbFixture;
        _respository = new MessagesRepository(localDynamoDbFixture.DynamoDbClient);
        _channelRespository = new ChannelRepository(localDynamoDbFixture.DynamoDbClient);
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
    public async Task CanSendAndRecieveMessages()
    {
        var channel1 = await _channelRespository.CreateChannel("CH1");
        var (conn1, content1) = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        var (conn2, content2) = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var channel2 = await _channelRespository.CreateChannel("CH2");
        var (conn3, content3) = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        var (conn4, content4) = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        await _respository.InsertMessage(conn1, channel1, content1);
        await _respository.InsertMessage(conn2, channel1, content2);
        await _respository.InsertMessage(conn3, channel2, content3);
        await _respository.InsertMessage(conn4, channel2, content4);

        var channel1Messages = await _respository.GetMessages(channel1);
        _ = channel1Messages.Should().HaveCount(2);
        _ = channel1Messages.Select(m => m.Sequence).Should().BeEquivalentTo(new[] { 0, 1 });

        var channel2Messages = await _respository.GetMessages(channel2);
        _ = channel2Messages.Should().HaveCount(2);
        _ = channel2Messages.Select(m => m.Sequence).Should().BeEquivalentTo(new[] { 0, 1 });
    }

    [Fact]
    public async Task SequenceGenerationIsCorrectUnderConcurrency()
    {
        var channel1 = await _channelRespository.CreateChannel("CH1");

        var tasks = new List<Task>();
        for (var i = 0; i < 10; i++)
        {
            var conn = Guid.NewGuid().ToString();
            var content = Guid.NewGuid().ToString();
            tasks.Add(_respository.InsertMessage(conn, channel1, content));
        }

        await Task.WhenAll(tasks);

        var messages = await _respository.GetMessages(channel1);
        _ = messages.Select(m => m.Sequence).Should().BeEquivalentTo(Enumerable.Range(0, 10));

    }

    [Fact]
    public async Task GetFetchMessagesCorrectly()
    {
        var channel = await _channelRespository.CreateChannel("CH1");
        for (var i = 0; i < 10; i++)
        {
            await _respository.InsertMessage($"connection-{i}", channel, $"message-{i}");
        }

        var messages = await _respository.FetchMessages(channel, 10, null);
        _ = messages.Select(m => m.Content).Should().BeEquivalentTo(Enumerable.Range(0, 10).Select(i => $"message-{i}"), options => options.WithStrictOrdering());

        messages = await _respository.FetchMessages(channel, 2, 9);
        _ = messages.Select(m => m.Content).Should().BeEquivalentTo(new[] { "message-8", "message-9" }, options => options.WithStrictOrdering());

        messages = await _respository.FetchMessages(channel, 10, 0);
        _ = messages.Select(m => m.Content).Should().BeEquivalentTo(new[] { "message-0" });
    }
}
