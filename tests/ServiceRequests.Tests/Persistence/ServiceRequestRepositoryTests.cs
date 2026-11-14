using System.Data.SqlTypes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Core;
using ServiceRequests.Infrastructure;

namespace ServiceRequests.Tests.Persistence;

/// <summary>
/// Integration tests of the real repository (resolved through <c>AddInfrastructure</c>) against SQL Server LocalDB:
/// collation-based identity, UNIQUE race signal, timestamps, SQL filtering and ordering.
/// </summary>
[Collection(LocalDbCollection.Name)]
public class ServiceRequestRepositoryTests(LocalDbFixture database) : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _services = new ServiceCollection()
        .AddInfrastructure(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ServiceRequests"] = database.ConnectionString
            })
            .Build())
        .BuildServiceProvider();

    public void Dispose() => _services.Dispose();

    [Fact]
    public async Task Migration_AddRequestTimestamps_CreatesRequiredDateTimeOffsetColumns()
    {
        await using var context = database.CreateContext();

        var applied = await context.Database.GetAppliedMigrationsAsync();
        var columns = await context.Database
            .SqlQuery<ColumnRow>($"""
                SELECT c.name AS Name, t.name AS TypeName, c.is_nullable AS IsNullable
                FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
                WHERE c.object_id = OBJECT_ID(N'dbo.ServiceRequests') AND c.name IN (N'CreatedAt', N'UpdatedAt')
                """)
            .ToListAsync();

        Assert.Contains(applied, m => m.EndsWith("_InitialCreate"));
        Assert.Contains(applied, m => m.EndsWith("_AddRequestTimestamps"));
        Assert.Equal(2, columns.Count);
        Assert.All(columns, c => Assert.Equal(("datetimeoffset", false), (c.TypeName, c.IsNullable)));
    }

    [Fact]
    public async Task CaseVariantReference_AsDistinctRow_IsRejectedByUniqueIndex()
    {
        var reference = $"CI-{Guid.NewGuid():N}".ToUpperInvariant();
        await using (var context = database.CreateContext())
        {
            context.ServiceRequests.Add(new ServiceRequest(reference, "CUST-01", "INST", "first", Now));
            await context.SaveChangesAsync();
        }

        await using var second = database.CreateContext();
        second.ServiceRequests.Add(
            new ServiceRequest(reference.ToLowerInvariant(), "CUST-01", "INST", "case variant", Now));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
        Assert.Contains(Assert.IsType<SqlException>(exception.InnerException).Number, new[] { 2601, 2627 });
    }

    [Fact]
    public async Task TryAdd_CaseVariantDuplicate_ReturnsFalseAndKeepsSingleRow()
    {
        var reference = $"RACE-{Guid.NewGuid():N}".ToUpperInvariant();
        Assert.True(await AddAsync(new ServiceRequest(reference, "CUST-01", "INST", "winner", Now)));

        var added = await AddAsync(new ServiceRequest(reference.ToLowerInvariant(), "CUST-01", "INST", "loser", Now));

        Assert.False(added);
        await using var verify = database.CreateContext();
        var stored = await verify.ServiceRequests.Where(r => r.ExternalReference == reference).ToListAsync();
        Assert.Equal("winner", Assert.Single(stored).Description);
    }

    [Fact]
    public async Task FindByExternalReference_WithDifferentCase_ReturnsStoredRequestWithOriginalCasing()
    {
        var reference = $"Find-{Guid.NewGuid():N}";
        Assert.True(await AddAsync(new ServiceRequest(reference, "CUST-01", "INST", "stored", Now)));

        using var scope = _services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IServiceRequestRepository>();
        var found = await repository.FindByExternalReferenceAsync(reference.ToUpperInvariant(), CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(reference, found.ExternalReference);
    }

    [Fact]
    public async Task Timestamps_SurviveRoundTripExactly()
    {
        var request = new ServiceRequest($"TS-{Guid.NewGuid():N}", "CUST-01", "INST", "timestamps", Now);
        request.MarkProcessing(Now.AddSeconds(3));
        Assert.True(await AddAsync(request));

        await using var verify = database.CreateContext();
        var loaded = await verify.ServiceRequests.AsNoTracking().SingleAsync(r => r.Id == request.Id);

        Assert.Equal(Now, loaded.CreatedAt);
        Assert.Equal(Now.AddSeconds(3), loaded.UpdatedAt);
        Assert.Equal(TimeSpan.Zero, loaded.UpdatedAt.Offset);
    }

    [Fact]
    public async Task List_FiltersByStatusInSqlAndOrdersByCreatedAtThenIdDescending()
    {
        var batch = $"LIST-{Guid.NewGuid():N}";
        var oldest = new ServiceRequest($"{batch}-1", "C", "S", "oldest", Now);
        var tieA = new ServiceRequest($"{batch}-2", "C", "S", "tie a", Now.AddMinutes(1));
        var tieB = new ServiceRequest($"{batch}-3", "C", "S", "tie b", Now.AddMinutes(1));
        var newestCompleted = new ServiceRequest($"{batch}-4", "C", "S", "completed", Now.AddMinutes(2));
        newestCompleted.MarkProcessing(Now.AddMinutes(2));
        newestCompleted.Complete(Now.AddMinutes(2));
        foreach (var request in new[] { oldest, tieA, tieB, newestCompleted })
        {
            Assert.True(await AddAsync(request));
        }

        using var scope = _services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IServiceRequestRepository>();
        var pending = Own(await repository.ListAsync(ServiceRequestStatus.Pending, CancellationToken.None), batch);
        var all = Own(await repository.ListAsync(null, CancellationToken.None), batch);

        // SQL Server orders uniqueidentifier values with SqlGuid semantics, not System.Guid.
        var ties = new[] { tieA, tieB }.OrderByDescending(r => new SqlGuid(r.Id)).Select(r => r.Id).ToArray();
        Assert.Equal([ties[0], ties[1], oldest.Id], pending.Select(r => r.Id));
        Assert.All(pending, r => Assert.Equal(ServiceRequestStatus.Pending, r.Status));
        Assert.Equal([newestCompleted.Id, ties[0], ties[1], oldest.Id], all.Select(r => r.Id));
    }

    private async Task<bool> AddAsync(ServiceRequest request)
    {
        using var scope = _services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IServiceRequestRepository>();
        return await repository.TryAddAsync(request, CancellationToken.None);
    }

    private static List<ServiceRequest> Own(IEnumerable<ServiceRequest> requests, string batch) =>
        requests.Where(r => r.ExternalReference.StartsWith(batch, StringComparison.Ordinal)).ToList();

    private sealed class ColumnRow
    {
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
    }
}
