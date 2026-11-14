using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceRequests.Core;
using ServiceRequests.Infrastructure.Persistence;

namespace ServiceRequests.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "ServiceRequests";

    /// <summary>
    /// Registers persistence (DbContext, <see cref="IServiceRequestRepository"/>) and the system
    /// <see cref="TimeProvider"/>. The connection string comes from <c>ConnectionStrings:ServiceRequests</c>
    /// (overridable with the <c>ConnectionStrings__ServiceRequests</c> environment variable); startup fails if it is missing.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured.");
        }

        services.AddLogging(); // idempotent; hosts already register it, plain service collections may not
        services.AddDbContext<ServiceRequestsDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
