using System.Reflection;
using ServiceRequests.Core;

namespace ServiceRequests.Tests.Domain;

public class ServiceRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    public enum Transition
    {
        MarkProcessing,
        Complete,
        Fail
    }

    [Fact]
    public void Constructor_WithValidValues_StartsPendingWithTrimmedValues()
    {
        var request = new ServiceRequest("  REQ-1 ", " CUST-9 ", " INSTALL ", "  Install meter  ", Now);

        Assert.Equal(ServiceRequestStatus.Pending, request.Status);
        Assert.Equal("REQ-1", request.ExternalReference);
        Assert.Equal("CUST-9", request.CustomerId);
        Assert.Equal("INSTALL", request.ServiceCode);
        Assert.Equal("Install meter", request.Description);
        Assert.Null(request.FailureReason);
        Assert.NotEqual(Guid.Empty, request.Id);
    }

    public static TheoryData<string?, string?, string?, string?, string> MissingRequiredValues => new()
    {
        { null, "CUST", "SVC", "desc", "externalReference" },
        { "", "CUST", "SVC", "desc", "externalReference" },
        { "   ", "CUST", "SVC", "desc", "externalReference" },
        { "REQ", null, "SVC", "desc", "customerId" },
        { "REQ", " ", "SVC", "desc", "customerId" },
        { "REQ", "CUST", null, "desc", "serviceCode" },
        { "REQ", "CUST", "\t", "desc", "serviceCode" },
        { "REQ", "CUST", "SVC", null, "description" },
        { "REQ", "CUST", "SVC", "", "description" },
    };

    [Theory]
    [MemberData(nameof(MissingRequiredValues))]
    public void Constructor_WithMissingRequiredValue_ThrowsForThatParameter(
        string? externalReference, string? customerId, string? serviceCode, string? description, string expectedParameter)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new ServiceRequest(externalReference!, customerId!, serviceCode!, description!, Now));

        Assert.Equal(expectedParameter, exception.ParamName);
    }

    [Fact]
    public void MarkProcessing_WhenPending_MovesToProcessing()
    {
        var request = NewRequest();

        request.MarkProcessing(Now);

        Assert.Equal(ServiceRequestStatus.Processing, request.Status);
    }

    [Fact]
    public void Complete_WhenProcessing_MovesToCompleted()
    {
        var request = InStatus(ServiceRequestStatus.Processing);

        request.Complete(Now);

        Assert.Equal(ServiceRequestStatus.Completed, request.Status);
        Assert.Null(request.FailureReason);
    }

    [Fact]
    public void Fail_WhenProcessing_MovesToFailedWithTrimmedReason()
    {
        var request = InStatus(ServiceRequestStatus.Processing);

        request.Fail("  provider unavailable  ", Now);

        Assert.Equal(ServiceRequestStatus.Failed, request.Status);
        Assert.Equal("provider unavailable", request.FailureReason);
    }

    [Fact]
    public void Fail_WithReasonLongerThanLimit_StoresTruncatedReason()
    {
        var request = InStatus(ServiceRequestStatus.Processing);

        request.Fail(new string('x', ServiceRequest.FailureReasonMaxLength + 50), Now);

        Assert.Equal(ServiceRequest.FailureReasonMaxLength, request.FailureReason!.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Fail_WithBlankReason_ThrowsAndKeepsProcessing(string? reason)
    {
        var request = InStatus(ServiceRequestStatus.Processing);

        Assert.Throws<ArgumentException>(() => request.Fail(reason!, Now));
        Assert.Equal(ServiceRequestStatus.Processing, request.Status);
    }

    // Every transition not in Pending→Processing, Processing→Completed, Processing→Failed.
    // Completed is terminal; Failed is final (assumption A-03).
    public static TheoryData<ServiceRequestStatus, Transition> InvalidTransitions => new()
    {
        { ServiceRequestStatus.Pending, Transition.Complete },
        { ServiceRequestStatus.Pending, Transition.Fail },
        { ServiceRequestStatus.Processing, Transition.MarkProcessing },
        { ServiceRequestStatus.Completed, Transition.MarkProcessing },
        { ServiceRequestStatus.Completed, Transition.Complete },
        { ServiceRequestStatus.Completed, Transition.Fail },
        { ServiceRequestStatus.Failed, Transition.MarkProcessing },
        { ServiceRequestStatus.Failed, Transition.Complete },
        { ServiceRequestStatus.Failed, Transition.Fail },
    };

    [Theory]
    [MemberData(nameof(InvalidTransitions))]
    public void Transition_NotAllowedFromCurrentStatus_ThrowsAndKeepsStatus(
        ServiceRequestStatus from, Transition transition)
    {
        var request = InStatus(from);
        var reasonBefore = request.FailureReason;

        Assert.Throws<InvalidOperationException>(() => Apply(request, transition));
        Assert.Equal(from, request.Status);
        Assert.Equal(reasonBefore, request.FailureReason);
    }

    [Fact]
    public void PublicProperties_HaveNoPublicSetters()
    {
        var publicSetters = typeof(ServiceRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod?.IsPublic == true)
            .Select(p => p.Name);

        Assert.Empty(publicSetters);
    }

    private static ServiceRequest NewRequest() => new("REQ-1", "CUST-1", "SVC-1", "Description", Now);

    private static ServiceRequest InStatus(ServiceRequestStatus status)
    {
        var request = NewRequest();
        switch (status)
        {
            case ServiceRequestStatus.Pending:
                break;
            case ServiceRequestStatus.Processing:
                request.MarkProcessing(Now);
                break;
            case ServiceRequestStatus.Completed:
                request.MarkProcessing(Now);
                request.Complete(Now);
                break;
            case ServiceRequestStatus.Failed:
                request.MarkProcessing(Now);
                request.Fail("transient failures exhausted", Now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        Assert.Equal(status, request.Status);
        return request;
    }

    private static void Apply(ServiceRequest request, Transition transition)
    {
        switch (transition)
        {
            case Transition.MarkProcessing:
                request.MarkProcessing(Now);
                break;
            case Transition.Complete:
                request.Complete(Now);
                break;
            case Transition.Fail:
                request.Fail("another failure", Now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(transition), transition, null);
        }
    }
}
