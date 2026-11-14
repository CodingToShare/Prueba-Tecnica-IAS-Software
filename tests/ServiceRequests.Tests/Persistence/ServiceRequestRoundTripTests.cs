using Microsoft.EntityFrameworkCore;
using ServiceRequests.Core;

namespace ServiceRequests.Tests.Persistence;

/// <summary>
/// Functional tests of the Phase 01 boundary: domain entity → EF Core → real SQL Server → fresh context.
/// </summary>
[Collection(LocalDbCollection.Name)]
public class ServiceRequestRoundTripTests(LocalDbFixture database)
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task NewRequest_SavedAndReloadedInFreshContext_KeepsBusinessDataAndPendingStatus()
    {
        var reference = ServiceRequestPersistenceTests.UniqueReference();
        var created = new ServiceRequest($"  {reference}  ", " CUST-42 ", " METER-INSTALL ", " Install meter at site ", Now);

        await using (var writeContext = database.CreateContext())
        {
            writeContext.ServiceRequests.Add(created);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = database.CreateContext();
        var loaded = await readContext.ServiceRequests.AsNoTracking().SingleAsync(r => r.Id == created.Id);

        Assert.Equal(reference, loaded.ExternalReference);
        Assert.Equal("CUST-42", loaded.CustomerId);
        Assert.Equal("METER-INSTALL", loaded.ServiceCode);
        Assert.Equal("Install meter at site", loaded.Description);
        Assert.Equal(ServiceRequestStatus.Pending, loaded.Status);
        Assert.Null(loaded.FailureReason);
    }

    [Fact]
    public async Task QueryByStatus_ReturnsOnlyRequestsInThatStatus()
    {
        var batch = $"QS-{Guid.NewGuid():N}";
        var pending = new ServiceRequest($"{batch}-pending", "CUST-1", "SVC-1", "stays pending", Now);
        var processing = new ServiceRequest($"{batch}-processing", "CUST-1", "SVC-1", "claimed", Now);
        processing.MarkProcessing(Now);
        var completed = new ServiceRequest($"{batch}-completed", "CUST-1", "SVC-1", "done", Now);
        completed.MarkProcessing(Now);
        completed.Complete(Now);

        await using (var writeContext = database.CreateContext())
        {
            writeContext.ServiceRequests.AddRange(pending, processing, completed);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = database.CreateContext();
        var pendingReferences = await readContext.ServiceRequests.AsNoTracking()
            .Where(r => r.Status == ServiceRequestStatus.Pending && r.ExternalReference.StartsWith(batch))
            .Select(r => r.ExternalReference)
            .ToListAsync();

        Assert.Equal([pending.ExternalReference], pendingReferences);
    }
}
