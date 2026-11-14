using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceRequests.Infrastructure;
using ServiceRequests.Tests.Persistence;
using ServiceRequests.Worker;

namespace ServiceRequests.Tests.Worker;

/// <summary>
/// Separate per-run LocalDB database for Worker tests: the Worker processes every Pending row, so it must not share
/// a database with the API/persistence tests that leave Pending rows behind.
/// </summary>
[CollectionDefinition(Name)]
public sealed class WorkerDbCollection : ICollectionFixture<LocalDbFixture>
{
    public const string Name = "WorkerDb";
}

internal static class WorkerServices
{
    /// <summary>Production composition (<c>AddInfrastructure</c> + <c>AddRequestProcessing</c>) on the test database.</summary>
    public static ServiceProvider Build(
        string connectionString, int transientFailuresBeforeSuccess, ILoggerProvider? logs = null) =>
        new ServiceCollection()
            .AddLogging(builder =>
            {
                if (logs is not null)
                {
                    builder.AddProvider(logs);
                }
            })
            .AddInfrastructure(Configuration(connectionString, transientFailuresBeforeSuccess))
            .AddRequestProcessing(Configuration(connectionString, transientFailuresBeforeSuccess))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    private static IConfiguration Configuration(string connectionString, int transientFailuresBeforeSuccess) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ServiceRequests"] = connectionString,
                ["Worker:PollingIntervalMilliseconds"] = "100",
                ["Worker:MaxRetryAttempts"] = "2",
                ["Worker:RetryDelayMilliseconds"] = "0",
                ["ExternalProvider:TransientFailuresBeforeSuccess"] = transientFailuresBeforeSuccess.ToString(),
                ["ExternalProvider:PermanentFailureServiceCodes:0"] = "UNSUPPORTED",
            })
            .Build();
}
