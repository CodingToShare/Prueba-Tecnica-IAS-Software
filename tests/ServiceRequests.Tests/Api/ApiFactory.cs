using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceRequests.Tests.Api;

/// <summary>Real API pipeline pointed at a test database; optionally replaces services for fault injection.</summary>
internal sealed class ApiFactory(
    string connectionString,
    string environment = "Testing",
    Action<IServiceCollection>? configureServices = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:ServiceRequests", connectionString);
        if (configureServices is not null)
        {
            builder.ConfigureTestServices(configureServices);
        }
    }
}
