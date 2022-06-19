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

    public MessagesRepositoryTest(LocalDynamoDbFixture localDynamoDbFixture)
    {
        _localDynamoDbFixture = localDynamoDbFixture;
        _respository = new MessagesRepository(localDynamoDbFixture.DynamoDbClient);
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
