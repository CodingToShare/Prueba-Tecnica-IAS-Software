using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ServiceRequests.Core;
using ServiceRequests.Tests.Persistence;

namespace ServiceRequests.Tests.Api;

/// <summary>
/// Functional HTTP tests through the real ASP.NET Core pipeline (WebApplicationFactory) and SQL Server LocalDB.
/// </summary>
[Collection(LocalDbCollection.Name)]
public sealed class ServiceRequestsApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly LocalDbFixture _database;
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ServiceRequestsApiTests(LocalDbFixture database)
    {
        _database = database;
        _factory = new ApiFactory(database.ConnectionString);
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Post_NewRequest_Returns201WithPendingStatusAndServerTimestamps()
    {
        var reference = NewReference();

        var response = await PostAsync(reference, "CUST-01", "INST", "Install service");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadAsync<RequestBody>(response);
        Assert.Equal(reference, body.ExternalReference);
        Assert.Equal("Pending", body.Status);
        Assert.NotEqual(default, body.CreatedAt);
        Assert.Equal(body.CreatedAt, body.UpdatedAt);
        Assert.Equal(1, await CountRowsAsync(reference));
    }

    [Fact]
    public async Task Post_SameRequestAgain_Returns200WithSameIdAndNoSecondRow()
    {
        var reference = NewReference();
        var created = await ReadAsync<RequestBody>(await PostAsync(reference, "CUST-01", "INST", "Install service"));

        var replay = await PostAsync(reference, "CUST-01", "INST", "Install service");

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(created.Id, (await ReadAsync<RequestBody>(replay)).Id);
        Assert.Equal(1, await CountRowsAsync(reference));
    }

    [Fact]
    public async Task Post_CaseVariantReferenceWithSamePayload_Returns200WithSameId()
    {
        var reference = NewReference();
        var created = await ReadAsync<RequestBody>(await PostAsync(reference, "CUST-01", "INST", "Install service"));

        var replay = await PostAsync($"  {reference.ToLowerInvariant()} ", "CUST-01", "INST", "Install service");

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var body = await ReadAsync<RequestBody>(replay);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal(reference, body.ExternalReference);
        Assert.Equal(1, await CountRowsAsync(reference));
    }

    [Fact]
    public async Task Post_SameReferenceWithDifferentPayload_Returns409ProblemAndKeepsOriginal()
    {
        var reference = NewReference();
        await PostAsync(reference, "CUST-01", "INST", "Install service");

        var conflict = await PostAsync(reference.ToLowerInvariant(), "cust-01", "INST", "Install service");

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType?.MediaType);
        var problem = await ReadAsync<ProblemBody>(conflict);
        Assert.Equal(409, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));

        await using var context = _database.CreateContext();
        var stored = await context.ServiceRequests.AsNoTracking().SingleAsync(r => r.ExternalReference == reference);
        Assert.Equal("CUST-01", stored.CustomerId);
    }

    public static TheoryData<string, string> InvalidBodies => new()
    {
        { """{"customerId":"C","serviceCode":"S","description":"d"}""", "ExternalReference" },
        { """{"externalReference":"   ","customerId":"C","serviceCode":"S","description":"d"}""", "ExternalReference" },
        { """{"externalReference":"R","customerId":null,"serviceCode":"S","description":"d"}""", "CustomerId" },
        { $$"""{"externalReference":"R","customerId":"C","serviceCode":"{{new string('s', 51)}}","description":"d"}""", "ServiceCode" },
        { """{"externalReference":"R","customerId":"C","serviceCode":"S"}""", "Description" },
    };

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task Post_InvalidBody_Returns400ValidationProblemForField(string json, string field)
    {
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/requests", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadAsync<ProblemBody>(response);
        Assert.NotNull(problem.Errors);
        Assert.Contains(field, problem.Errors.Keys);
    }

    [Fact]
    public async Task Get_WithoutFilter_ReturnsPersistedRequests()
    {
        var reference = NewReference();
        await PostAsync(reference, "CUST-01", "INST", "Install service");

        var list = await _client.GetFromJsonAsync<List<RequestBody>>("/api/requests", Json);

        Assert.Contains(list!, r => r.ExternalReference == reference && r.Status == "Pending");
    }

    [Fact]
    public async Task Get_WithStatusFilter_ReturnsOnlyRequestsInThatStatus()
    {
        var pendingReference = NewReference();
        var processingReference = NewReference();
        await PostAsync(pendingReference, "CUST-01", "INST", "stays pending");
        await PostAsync(processingReference, "CUST-01", "INST", "gets claimed");
        await using (var context = _database.CreateContext())
        {
            var claimed = await context.ServiceRequests.SingleAsync(r => r.ExternalReference == processingReference);
            claimed.MarkProcessing(DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        }

        var pending = await _client.GetFromJsonAsync<List<RequestBody>>("/api/requests?status=Pending", Json);
        var processing = await _client.GetFromJsonAsync<List<RequestBody>>("/api/requests?status=processing", Json);

        Assert.All(pending!, r => Assert.Equal("Pending", r.Status));
        Assert.Contains(pending!, r => r.ExternalReference == pendingReference);
        Assert.DoesNotContain(pending!, r => r.ExternalReference == processingReference);
        Assert.All(processing!, r => Assert.Equal("Processing", r.Status));
        Assert.Contains(processing!, r => r.ExternalReference == processingReference);
    }

    [Fact]
    public async Task Post_PaddedValueThatFitsAfterTrim_IsAcceptedAndStoredTrimmed()
    {
        // Regression for the Phase 02.2 fix: length limits apply to the trimmed value.
        var reference = NewReference().PadRight(ServiceRequest.ExternalReferenceMaxLength, 'X');

        var accepted = await PostAsync($"   {reference}   ", "CUST-01", "INST", "Install service");
        var tooLong = await PostAsync(reference + "Y", "CUST-01", "INST", "Install service");

        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        Assert.Equal(reference, (await ReadAsync<RequestBody>(accepted)).ExternalReference);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
    }

    [Theory]
    [InlineData("Banana")]
    [InlineData("1")]
    public async Task Get_WithUnknownStatus_Returns400InsteadOfEmptyList(string status)
    {
        var response = await _client.GetAsync($"/api/requests?status={status}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadAsync<ProblemBody>(response);
        Assert.Contains("status", problem.Errors!.Keys);
    }

    private static string NewReference() => $"HTTP-{Guid.NewGuid():N}".ToUpperInvariant();

    private Task<HttpResponseMessage> PostAsync(
        string externalReference, string customerId, string serviceCode, string description) =>
        _client.PostAsJsonAsync("/api/requests", new { externalReference, customerId, serviceCode, description }, Json);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    private async Task<int> CountRowsAsync(string reference)
    {
        await using var context = _database.CreateContext();
        return await context.ServiceRequests.CountAsync(r => r.ExternalReference == reference);
    }

    private sealed record RequestBody(
        Guid Id,
        string ExternalReference,
        string CustomerId,
        string ServiceCode,
        string Description,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record ProblemBody(int Status, string? Title, Dictionary<string, string[]>? Errors);
}
