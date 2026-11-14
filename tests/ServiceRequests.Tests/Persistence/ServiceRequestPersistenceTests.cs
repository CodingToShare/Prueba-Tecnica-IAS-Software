using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ServiceRequests.Core;

namespace ServiceRequests.Tests.Persistence;

/// <summary>Integration tests against real SQL Server (LocalDB): schema, constraints and mapping.</summary>
[Collection(LocalDbCollection.Name)]
public class ServiceRequestPersistenceTests(LocalDbFixture database)
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Migrations_OnCleanDatabase_AreAllApplied()
    {
        await using var context = database.CreateContext();

        var pending = await context.Database.GetPendingMigrationsAsync();
        var applied = await context.Database.GetAppliedMigrationsAsync();

        Assert.Empty(pending);
        Assert.Equal(context.Database.GetMigrations(), applied);
    }

    [Fact]
    public async Task Schema_HasUniqueExternalReferenceIndexAndStatusIndex()
    {
        await using var context = database.CreateContext();

        var indexes = await context.Database
            .SqlQuery<IndexRow>($"""
                SELECT i.name AS Name, c.name AS ColumnName, i.is_unique AS IsUnique
                FROM sys.indexes i
                JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE i.object_id = OBJECT_ID(N'dbo.ServiceRequests') AND i.is_primary_key = 0
                """)
            .ToListAsync();

        Assert.Contains(indexes, i => i is { ColumnName: "ExternalReference", IsUnique: true });
        Assert.Contains(indexes, i => i is { ColumnName: "Status", IsUnique: false });
    }

    [Fact]
    public async Task SaveChanges_WithDuplicateExternalReference_IsRejectedByUniqueIndex()
    {
        var reference = UniqueReference();
        await using (var context = database.CreateContext())
        {
            context.ServiceRequests.Add(new ServiceRequest(reference, "CUST-1", "SVC-1", "first", Now));
            await context.SaveChangesAsync();
        }

        await using var second = database.CreateContext();
        second.ServiceRequests.Add(new ServiceRequest(reference, "CUST-2", "SVC-2", "second, distinct entity", Now));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());

        // 2601: duplicate key in unique index; 2627: unique constraint violation.
        var sqlException = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains(sqlException.Number, new[] { 2601, 2627 });

        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.ServiceRequests.CountAsync(r => r.ExternalReference == reference));
    }

    [Fact]
    public async Task SaveChanges_WithFailedRequest_PersistsStatusAsTextAndReason()
    {
        var request = new ServiceRequest(UniqueReference(), "CUST-1", "SVC-1", "will fail", Now);
        request.MarkProcessing(Now);
        request.Fail("transient failures exhausted", Now);

        await using (var context = database.CreateContext())
        {
            context.ServiceRequests.Add(request);
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var storedStatus = await verify.Database
            .SqlQuery<string>($"SELECT Status AS Value FROM dbo.ServiceRequests WHERE Id = {request.Id}")
            .SingleAsync();
        var loaded = await verify.ServiceRequests.SingleAsync(r => r.Id == request.Id);

        Assert.Equal("Failed", storedStatus);
        Assert.Equal(ServiceRequestStatus.Failed, loaded.Status);
        Assert.Equal("transient failures exhausted", loaded.FailureReason);
    }

    internal static string UniqueReference() => $"IT-{Guid.NewGuid():N}";

    private sealed class IndexRow
    {
        public string Name { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public bool IsUnique { get; set; }
    }
}
