using ServiceRequests.Core;

namespace ServiceRequests.Tests.Domain;

public class ServiceRequestTimestampAndReplayTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_SetsCreatedAndUpdatedToNowInUtc()
    {
        var localNow = new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.FromHours(-5));

        var request = new ServiceRequest("REQ-1", "CUST-1", "SVC-1", "Description", localNow);

        Assert.Equal(localNow, request.CreatedAt);
        Assert.Equal(TimeSpan.Zero, request.CreatedAt.Offset);
        Assert.Equal(request.CreatedAt, request.UpdatedAt);
    }

    [Fact]
    public void Transitions_UpdateUpdatedAtAndKeepCreatedAt()
    {
        var request = new ServiceRequest("REQ-1", "CUST-1", "SVC-1", "Description", CreatedAt);

        request.MarkProcessing(CreatedAt.AddSeconds(5));
        Assert.Equal(CreatedAt.AddSeconds(5), request.UpdatedAt);

        request.Fail("transient failures exhausted", CreatedAt.AddSeconds(9));

        Assert.Equal(CreatedAt, request.CreatedAt);
        Assert.Equal(CreatedAt.AddSeconds(9), request.UpdatedAt);
    }

    [Fact]
    public void RejectedTransition_DoesNotChangeUpdatedAt()
    {
        var request = new ServiceRequest("REQ-1", "CUST-1", "SVC-1", "Description", CreatedAt);

        Assert.Throws<InvalidOperationException>(() => request.Complete(CreatedAt.AddMinutes(1)));
        Assert.Equal(CreatedAt, request.UpdatedAt);
    }

    public static TheoryData<string, string, string, bool> ReplayPayloads => new()
    {
        { "CUST-01", "INST", "Install service", true },
        { "  CUST-01 ", " INST", "Install service  ", true },
        { "cust-01", "INST", "Install service", false },
        { "CUST-01", "inst", "Install service", false },
        { "CUST-01", "INST", "install service", false },
        { "CUST-02", "INST", "Install service", false },
        { "CUST-01", "INST", "Install service now", false },
    };

    [Theory]
    [MemberData(nameof(ReplayPayloads))]
    public void HasEquivalentPayload_ComparesTrimmedValuesOrdinally(
        string customerId, string serviceCode, string description, bool expected)
    {
        var existing = new ServiceRequest("REQ-1", "CUST-01", "INST", "Install service", CreatedAt);

        Assert.Equal(expected, existing.HasEquivalentPayload(customerId, serviceCode, description));
    }
}
