using Amazon.DynamoDBv2;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.WaitStrategies;
using Xunit;

namespace AwsServerlessChatroom.Tests;
public class LocalDynamoDbFixture : IAsyncLifetime
{
    private TestcontainersContainer? _testcontainers;
    private AmazonDynamoDBClient? _dynamoDbClient;

    public AmazonDynamoDBClient DynamoDbClient => _dynamoDbClient!;

    public async Task InitializeAsync()
    {
        _testcontainers = new TestcontainersBuilder<TestcontainersContainer>()
                 .WithImage("amazon/dynamodb-local:1.18.0")
                 .WithName("dynamodb-local")
                 .WithWorkingDirectory("/home/dynamodblocal")
                 .WithCommand("-jar DynamoDBLocal.jar -inMemory".Split(' '))
                 .WithPortBinding(8000, assignRandomHostPort: true)
                 .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8000))
                 .Build();
        await _testcontainers.StartAsync();

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
}
