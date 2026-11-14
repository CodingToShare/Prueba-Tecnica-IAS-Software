using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ServiceRequests.Infrastructure.Persistence;

namespace ServiceRequests.Tests.Persistence;

/// <summary>
/// Creates a uniquely named SQL Server database for the test run by applying the committed migrations,
/// and drops it afterwards (TD-17). Server defaults to LocalDB; override with the
/// <c>SERVICEREQUESTS_TEST_SQLSERVER</c> environment variable (a connection string without a database).
/// </summary>
public sealed class LocalDbFixture : IAsyncLifetime
{
    private const string ServerOverrideVariable = "SERVICEREQUESTS_TEST_SQLSERVER";
    private const string DefaultServer = @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True";

    public LocalDbFixture()
    {
        var builder = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable(ServerOverrideVariable) ?? DefaultServer)
        {
            InitialCatalog = $"ServiceRequestsDb_Tests_{Guid.NewGuid():N}"
        };
        ConnectionString = builder.ConnectionString;
    }

    public string ConnectionString { get; }

    public ServiceRequestsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ServiceRequestsDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class LocalDbCollection : ICollectionFixture<LocalDbFixture>
{
    public const string Name = "LocalDb";
}
