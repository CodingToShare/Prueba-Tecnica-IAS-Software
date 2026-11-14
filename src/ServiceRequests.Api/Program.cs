using System.Text.Json.Serialization;
using ServiceRequests.Core;
using ServiceRequests.Infrastructure;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// RFC 7807 responses for validation errors, explicit Problem() results and unhandled exceptions.
builder.Services.AddProblemDetails();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ServiceRequestService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .WithMethods("GET", "POST")
    .WithHeaders("Content-Type")));

var app = builder.Build();

// Unhandled exceptions → logged server-side, generic 500 ProblemDetails (no stack trace, SQL or connection details).
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors(FrontendCorsPolicy);

app.MapControllers();

app.Run();
