using Microsoft.EntityFrameworkCore;
using ServiceRequests.Core;

namespace ServiceRequests.Tests.Persistence;

/// <summary>
/// Functional tests of the Phase 01 boundary: domain entity → EF Core → real SQL Server → fresh context.
/// </summary>
[Collection(LocalDbCollection.Name)]
public class ServiceRequestRoundTripTests(LocalDbFixture database)
{
    [Fact]
    public async Task NewRequest_SavedAndReloadedInFreshContext_KeepsBusinessDataAndPendingStatus()
    {
        var reference = ServiceRequestPersistenceTests.UniqueReference();
        var created = new ServiceRequest($"  {reference}  ", " CUST-42 ", " METER-INSTALL ", " Install meter at site ");

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
        var pending = new ServiceRequest($"{batch}-pending", "CUST-1", "SVC-1", "stays pending");
        var processing = new ServiceRequest($"{batch}-processing", "CUST-1", "SVC-1", "claimed");
        processing.MarkProcessing();
        var completed = new ServiceRequest($"{batch}-completed", "CUST-1", "SVC-1", "done");
        completed.MarkProcessing();
        completed.Complete();

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
