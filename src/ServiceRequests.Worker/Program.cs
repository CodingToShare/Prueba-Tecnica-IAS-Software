using ServiceRequests.Infrastructure;
using ServiceRequests.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Runs as a console app with `dotnet run`; when started by the Service Control Manager it runs as a Windows Service.
builder.Services.AddWindowsService(options => options.ServiceName = "ServiceRequestsWorker");

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRequestProcessing(builder.Configuration);

var host = builder.Build();
host.Run();
