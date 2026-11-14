using Microsoft.EntityFrameworkCore;
using ServiceRequests.Core;

namespace ServiceRequests.Infrastructure.Persistence;

public sealed class ServiceRequestsDbContext(DbContextOptions<ServiceRequestsDbContext> options) : DbContext(options)
{
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServiceRequestsDbContext).Assembly);
    }
}
