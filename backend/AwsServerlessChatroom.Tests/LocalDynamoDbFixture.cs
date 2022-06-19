using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AwsServerlessChatroom.DataAccess;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.WaitStrategies;
using Xunit;

namespace AwsServerlessChatroom.Tests;
public class LocalDynamoDbFixture : IAsyncLifetime
{
    private static readonly SemaphoreSlim Semaphore = new(1);
    private TestcontainersContainer? _testcontainers;
    private AmazonDynamoDBClient? _dynamoDbClient;

    public AmazonDynamoDBClient DynamoDbClient => _dynamoDbClient!;

    public async Task InitializeAsync()
    {
        // Serialize the containers startup to prevent overloading Docker which causes exceptions
        try
        {
            await Semaphore.WaitAsync();
            _testcontainers = new TestcontainersBuilder<TestcontainersContainer>()
                 .WithImage("amazon/dynamodb-local:1.18.0")
                 .WithName($"dynamodb-local-{Guid.NewGuid()}")
                 .WithWorkingDirectory("/home/dynamodblocal")
                 .WithCommand("-jar DynamoDBLocal.jar -inMemory".Split(' '))
                 .WithPortBinding(8000, assignRandomHostPort: true)
                 .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8000))
                 .Build();
            await _testcontainers.StartAsync();
        }
        finally
        {
            _ = Semaphore.Release();
        }

        _dynamoDbClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig
        {
            ServiceURL = $"http://localhost:{_testcontainers.GetMappedPublicPort(8000)}"
        });
    }

    public async Task DisposeAsync()
    {
        _dynamoDbClient?.Dispose();
        await (_testcontainers?.StopAsync() ?? Task.CompletedTask);
    }

    public async Task CreateTables()
    {
        _ = await DynamoDbClient.CreateTableAsync(new CreateTableRequest
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

        _ = await DynamoDbClient.CreateTableAsync(new CreateTableRequest
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

        _ = await DynamoDbClient.CreateTableAsync(new CreateTableRequest
        {
            TableName = DynamoDbTableNames.Messages,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement("ChannelId", KeyType.HASH),
                new KeySchemaElement("MsgSeq", KeyType.RANGE),
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition("ChannelId", ScalarAttributeType.S),
                new AttributeDefinition("MsgSeq", ScalarAttributeType.N),
            },
            ProvisionedThroughput = new ProvisionedThroughput(5, 5),
        });

        _ = await DynamoDbClient.CreateTableAsync(new CreateTableRequest
        {
            TableName = DynamoDbTableNames.MessageSequence,
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

    public async Task DeleteTables()
    {
        var tables = new[]
        {
            DynamoDbTableNames.Channels,
            DynamoDbTableNames.ChannelSubscriptions,
            DynamoDbTableNames.Messages,
            DynamoDbTableNames.MessageSequence,
        };

        foreach (var t in tables)
        {
            _ = await DynamoDbClient.DeleteTableAsync(new DeleteTableRequest
            {
                TableName = t,
            });

        }
    }
}
