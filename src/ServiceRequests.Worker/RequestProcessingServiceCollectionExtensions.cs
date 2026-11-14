using Microsoft.Extensions.Options;
using ServiceRequests.Core;
using ServiceRequests.Infrastructure.Provider;

namespace ServiceRequests.Worker;

public static class RequestProcessingServiceCollectionExtensions
{
    /// <summary>
    /// Registers background processing: validated options, simulated provider, Polly provider pipeline, processor and
    /// the hosted Worker. Requires <c>AddInfrastructure</c> (repository, <see cref="TimeProvider"/>).
    /// </summary>
    public static IServiceCollection AddRequestProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WorkerOptions>()
            .Bind(configuration.GetSection(WorkerOptions.SectionName))
            .Validate(o => o.PollingIntervalMilliseconds is >= 100 and <= 60_000, "Worker:PollingIntervalMilliseconds must be 100–60000.")
            .Validate(o => o.MaxRetryAttempts is >= 1 and <= 5, "Worker:MaxRetryAttempts must be 1–5.")
            .Validate(o => o.RetryDelayMilliseconds is >= 0 and <= 10_000, "Worker:RetryDelayMilliseconds must be 0–10000.")
            .ValidateOnStart();

        services.AddOptions<ExternalProviderOptions>()
            .Bind(configuration.GetSection(ExternalProviderOptions.SectionName))
            .Validate(o => o.TransientFailuresBeforeSuccess is >= 0 and <= 10, "ExternalProvider:TransientFailuresBeforeSuccess must be 0–10.")
            .ValidateOnStart();

        // The simulated provider keeps its idempotency state in memory, so it must be a single shared instance.
        services.AddSingleton<IExternalProvider, SimulatedExternalProvider>();
        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<WorkerOptions>>().Value;
            return ServiceRequestProcessor.CreateProviderPipeline(
                settings.MaxRetryAttempts, TimeSpan.FromMilliseconds(settings.RetryDelayMilliseconds));
        });
        services.AddScoped<ServiceRequestProcessor>();
        services.AddHostedService<RequestProcessingWorker>();

        return services;
    }
}
