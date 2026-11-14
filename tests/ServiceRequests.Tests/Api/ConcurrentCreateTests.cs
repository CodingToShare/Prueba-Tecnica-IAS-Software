using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceRequests.Tests.Persistence;
using ServiceRequests.Tests.TestSupport;
using Xunit.Abstractions;

namespace ServiceRequests.Tests.Api;

/// <summary>
/// Real concurrency against the real API pipeline and SQL Server LocalDB: simultaneous POSTs for one external reference
/// must converge on a single persisted request without server errors (UNIQUE index + race re-read).
/// </summary>
[Collection(LocalDbCollection.Name)]
public sealed class ConcurrentCreateTests(LocalDbFixture database, ITestOutputHelper output)
{
    private const int Concurrency = 5;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ConcurrentPosts_SamePayload_PersistOneRequestAndAllReturnItsId()
    {
        var logs = new CapturingLoggerProvider();
        await using var api = new ApiFactory(database.ConnectionString,
            configureServices: services => services.AddSingleton<ILoggerProvider>(logs));
        using var client = api.CreateClient();
        var reference = $"CONC-{Guid.NewGuid():N}";
        var payload = new { externalReference = reference, customerId = "CUS-1", serviceCode = "INST", description = "Same" };

        var responses = await SendSimultaneouslyAsync(Enumerable.Repeat(payload, Concurrency).ToArray<object>(), client);

        Assert.All(responses, r => Assert.True(
            r.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK, $"unexpected {(int)r.StatusCode}"));
        // Exactly one request inserts the row (201); every other one replays it (200), whatever the scheduling.
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        var ids = new HashSet<Guid>();
        foreach (var response in responses)
        {
            ids.Add((await response.Content.ReadFromJsonAsync<Body>(Json))!.Id);
        }

        Assert.Single(ids);
        Assert.Equal(1, await CountRowsAsync(reference));
        Report(logs, reference);
    }

    [Fact]
    public async Task ConcurrentPosts_DifferentPayloads_OneWinsOthersGet409AndWinnerIsNotOverwritten()
    {
        var logs = new CapturingLoggerProvider();
        await using var api = new ApiFactory(database.ConnectionString,
            configureServices: services => services.AddSingleton<ILoggerProvider>(logs));
        using var client = api.CreateClient();
        var reference = $"CONC-{Guid.NewGuid():N}";
        var customers = Enumerable.Range(1, Concurrency).Select(i => $"CUS-{i}").ToArray();
        var payloads = customers
            .Select(customer => (object)new { externalReference = reference, customerId = customer, serviceCode = "INST", description = "Conflict" })
            .ToArray();

        var responses = await SendSimultaneouslyAsync(payloads, client);

        Assert.All(responses, r => Assert.True(
            r.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict, $"unexpected {(int)r.StatusCode}"));
        var winner = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(Concurrency - 1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        // The stored row is the winner's payload (one of those submitted), never overwritten by a loser.
        var winnerCustomer = (await winner.Content.ReadFromJsonAsync<Body>(Json))!.CustomerId;
        Assert.Contains(winnerCustomer, customers);
        await using var context = database.CreateContext();
        var stored = await context.ServiceRequests.AsNoTracking().Where(r => r.ExternalReference == reference).ToListAsync();
        Assert.Equal(winnerCustomer, Assert.Single(stored).CustomerId);
        Report(logs, reference);
    }

    // All requests are created first and released together so they overlap in the server.
    private static async Task<HttpResponseMessage[]> SendSimultaneouslyAsync(object[] payloads, HttpClient client)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = payloads.Select(async payload =>
        {
            await start.Task;
            return await client.PostAsJsonAsync("/api/requests", payload, Json);
        }).ToArray();
        start.SetResult();
        return await Task.WhenAll(tasks);
    }

    private async Task<int> CountRowsAsync(string reference)
    {
        await using var context = database.CreateContext();
        return await context.ServiceRequests.CountAsync(r => r.ExternalReference == reference);
    }

    // Evidence for OF-02 (not asserted: whether the race branch runs depends on scheduling).
    private void Report(CapturingLoggerProvider logs, string reference)
    {
        var efErrors = logs.Entries.Count(e => e.Level >= LogLevel.Error);
        var replays = logs.Entries.Count(e => e.Message.Contains($"{reference} replayed"));
        var conflicts = logs.Entries.Count(e => e.Message.Contains($"{reference} rejected"));
        output.WriteLine($"Error-level log entries: {efErrors}; replay logs: {replays}; conflict logs: {conflicts}");
    }

    private sealed record Body(Guid Id, string CustomerId);
}
