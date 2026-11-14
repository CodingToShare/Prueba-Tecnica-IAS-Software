using Microsoft.Extensions.Options;
using ServiceRequests.Core;
using ServiceRequests.Infrastructure;
using ServiceRequests.Infrastructure.Provider;
using ServiceRequests.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Runs as a console app with `dotnet run`; when started by the Service Control Manager it runs as a Windows Service.
builder.Services.AddWindowsService(options => options.ServiceName = "ServiceRequestsWorker");

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOptions<WorkerOptions>()
    .Bind(builder.Configuration.GetSection(WorkerOptions.SectionName))
    .Validate(o => o.PollingIntervalMilliseconds is >= 100 and <= 60_000, "Worker:PollingIntervalMilliseconds must be 100–60000.")
    .Validate(o => o.MaxRetryAttempts is >= 1 and <= 5, "Worker:MaxRetryAttempts must be 1–5.")
    .Validate(o => o.RetryDelayMilliseconds is >= 0 and <= 10_000, "Worker:RetryDelayMilliseconds must be 0–10000.")
    .ValidateOnStart();

builder.Services.AddOptions<ExternalProviderOptions>()
    .Bind(builder.Configuration.GetSection(ExternalProviderOptions.SectionName))
    .Validate(o => o.TransientFailuresBeforeSuccess is >= 0 and <= 10, "ExternalProvider:TransientFailuresBeforeSuccess must be 0–10.")
    .ValidateOnStart();

// The simulated provider keeps its idempotency state in memory, so it must be a single shared instance.
builder.Services.AddSingleton<IExternalProvider, SimulatedExternalProvider>();
builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<IOptions<WorkerOptions>>().Value;
    return ServiceRequestProcessor.CreateProviderPipeline(
        settings.MaxRetryAttempts, TimeSpan.FromMilliseconds(settings.RetryDelayMilliseconds));
});
builder.Services.AddScoped<ServiceRequestProcessor>();
builder.Services.AddHostedService<RequestProcessingWorker>();

var host = builder.Build();
host.Run();
