using Moq;
using ServiceRequests.Core;

namespace ServiceRequests.Tests.Application;

/// <summary>Unit tests of the idempotent create flow with the repository boundary mocked.</summary>
public class ServiceRequestServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    private readonly Mock<IServiceRequestRepository> _repository = new(MockBehavior.Strict);
    private readonly ServiceRequestService _service;

    public ServiceRequestServiceTests()
    {
        _service = new ServiceRequestService(_repository.Object, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Create_NewReference_InsertsPendingRequestWithServerTimestamps()
    {
        ServiceRequest? inserted = null;
        _repository.Setup(r => r.FindByExternalReferenceAsync("REQ-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceRequest?)null);
        _repository.Setup(r => r.TryAddAsync(It.IsAny<ServiceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceRequest, CancellationToken>((request, _) => inserted = request)
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(" REQ-1 ", "CUST-01", "INST", "Install service", CancellationToken.None);

        Assert.Equal(CreateServiceRequestOutcome.Created, result.Outcome);
        Assert.Same(inserted, result.Request);
        Assert.Equal("REQ-1", result.Request.ExternalReference);
        Assert.Equal(ServiceRequestStatus.Pending, result.Request.Status);
        Assert.Equal(Now, result.Request.CreatedAt);
        Assert.Equal(Now, result.Request.UpdatedAt);
    }

    [Fact]
    public async Task Create_ExistingReferenceWithEquivalentPayload_ReturnsReplayWithoutInserting()
    {
        var existing = Existing();
        _repository.Setup(r => r.FindByExternalReferenceAsync("req-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.CreateAsync("req-1", " CUST-01 ", "INST", "Install service ", CancellationToken.None);

        Assert.Equal(CreateServiceRequestOutcome.Replayed, result.Outcome);
        Assert.Same(existing, result.Request);
        _repository.Verify(r => r.TryAddAsync(It.IsAny<ServiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    public static TheoryData<string, string, string> ConflictingPayloads => new()
    {
        { "cust-01", "INST", "Install service" },
        { "CUST-01", "REPAIR", "Install service" },
        { "CUST-01", "INST", "Install service now" },
    };

    [Theory]
    [MemberData(nameof(ConflictingPayloads))]
    public async Task Create_ExistingReferenceWithDifferentPayload_ReturnsConflictWithoutInserting(
        string customerId, string serviceCode, string description)
    {
        var existing = Existing();
        _repository.Setup(r => r.FindByExternalReferenceAsync("REQ-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.CreateAsync("REQ-1", customerId, serviceCode, description, CancellationToken.None);

        Assert.Equal(CreateServiceRequestOutcome.Conflict, result.Outcome);
        Assert.Same(existing, result.Request);
        _repository.Verify(r => r.TryAddAsync(It.IsAny<ServiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("CUST-01", CreateServiceRequestOutcome.Replayed)]
    [InlineData("CUST-99", CreateServiceRequestOutcome.Conflict)]
    public async Task Create_WhenUniqueRaceRejectsInsert_RereadsWinnerAndAppliesSameRule(
        string customerId, CreateServiceRequestOutcome expected)
    {
        var winner = Existing();
        _repository.SetupSequence(r => r.FindByExternalReferenceAsync("REQ-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceRequest?)null)
            .ReturnsAsync(winner);
        _repository.Setup(r => r.TryAddAsync(It.IsAny<ServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.CreateAsync("REQ-1", customerId, "INST", "Install service", CancellationToken.None);

        Assert.Equal(expected, result.Outcome);
        Assert.Same(winner, result.Request);
        _repository.Verify(r => r.FindByExternalReferenceAsync("REQ-1", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Create_WhenRaceRejectsInsertButWinnerIsMissing_Throws()
    {
        _repository.Setup(r => r.FindByExternalReferenceAsync("REQ-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceRequest?)null);
        _repository.Setup(r => r.TryAddAsync(It.IsAny<ServiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync("REQ-1", "CUST-01", "INST", "Install service", CancellationToken.None));
    }

    [Fact]
    public async Task Create_PassesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        _repository.Setup(r => r.FindByExternalReferenceAsync("REQ-1", cts.Token)).ReturnsAsync(Existing());

        await _service.CreateAsync("REQ-1", "CUST-01", "INST", "Install service", cts.Token);

        _repository.Verify(r => r.FindByExternalReferenceAsync("REQ-1", cts.Token), Times.Once);
    }

    private static ServiceRequest Existing() =>
        new("REQ-1", "CUST-01", "INST", "Install service", Now.AddMinutes(-5));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
