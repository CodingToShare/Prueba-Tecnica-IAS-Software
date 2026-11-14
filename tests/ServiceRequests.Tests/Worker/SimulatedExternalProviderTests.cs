using Microsoft.Extensions.Options;
using ServiceRequests.Core;
using ServiceRequests.Infrastructure.Provider;

namespace ServiceRequests.Tests.Worker;

public class SimulatedExternalProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task FailsTransientlyConfiguredTimesThenSucceeds(int failures)
    {
        var provider = Provider(failures);
        var request = Request("REQ-1");

        for (var i = 0; i < failures; i++)
        {
            await Assert.ThrowsAsync<TransientProviderException>(() => provider.ProcessAsync(request, CancellationToken.None));
        }

        await provider.ProcessAsync(request, CancellationToken.None);
        Assert.Equal(1, provider.GetEffectCount("REQ-1"));
    }

    [Fact]
    public async Task AttemptsAreCountedPerReference()
    {
        var provider = Provider(failures: 1);

        await Assert.ThrowsAsync<TransientProviderException>(() => provider.ProcessAsync(Request("REQ-A"), CancellationToken.None));
        await Assert.ThrowsAsync<TransientProviderException>(() => provider.ProcessAsync(Request("REQ-B"), CancellationToken.None));
        await provider.ProcessAsync(Request("REQ-A"), CancellationToken.None);

        Assert.Equal(1, provider.GetEffectCount("REQ-A"));
        Assert.Equal(0, provider.GetEffectCount("REQ-B"));
    }

    [Fact]
    public async Task ReplayOfProcessedReference_SucceedsWithoutSecondEffect()
    {
        var provider = Provider(failures: 0);
        await provider.ProcessAsync(Request("REQ-1"), CancellationToken.None);

        await provider.ProcessAsync(Request("req-1"), CancellationToken.None);

        Assert.Equal(1, provider.GetEffectCount("REQ-1"));
    }

    [Fact]
    public async Task ConfiguredServiceCode_FailsPermanently()
    {
        var provider = Provider(failures: 0, permanentCodes: ["UNSUPPORTED"]);

        await Assert.ThrowsAsync<PermanentProviderException>(
            () => provider.ProcessAsync(Request("REQ-1", serviceCode: "unsupported"), CancellationToken.None));
        Assert.Equal(0, provider.GetEffectCount("REQ-1"));
    }

    private static SimulatedExternalProvider Provider(int failures, string[]? permanentCodes = null) =>
        new(Options.Create(new ExternalProviderOptions
        {
            TransientFailuresBeforeSuccess = failures,
            PermanentFailureServiceCodes = permanentCodes ?? []
        }));

    private static ServiceRequest Request(string reference, string serviceCode = "INST") =>
        new(reference, "CUST-01", serviceCode, "Install service", Now);
}
