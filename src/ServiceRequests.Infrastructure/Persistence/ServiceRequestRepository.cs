using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceRequests.Core;

namespace ServiceRequests.Infrastructure.Persistence;

internal sealed class ServiceRequestRepository(
    ServiceRequestsDbContext context,
    ILogger<ServiceRequestRepository> logger) : IServiceRequestRepository
{
    // SQL Server: 2601 duplicate key in a unique index, 2627 unique constraint violation.
    private static readonly int[] UniqueViolationErrorNumbers = [2601, 2627];

    public Task<ServiceRequest?> FindByExternalReferenceAsync(
        string externalReference, CancellationToken cancellationToken) =>
        // Plain equality: SQL Server applies the column collation (case-insensitive) and can seek the UNIQUE index.
        context.ServiceRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.ExternalReference == externalReference, cancellationToken);

    public async Task<bool> TryAddAsync(ServiceRequest request, CancellationToken cancellationToken)
    {
        var entry = context.ServiceRequests.Add(request);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            entry.State = EntityState.Detached;
            // EF Core has already logged the failed command at Error level; this states that it was expected and handled.
            logger.LogInformation(
                "Idempotency race for {ExternalReference}: the UNIQUE index rejected a concurrent insert (SQL error {SqlError}); the existing request is re-read",
                request.ExternalReference, ((SqlException)exception.InnerException!).Number);
            return false;
        }
    }

    public async Task<IReadOnlyList<ServiceRequest>> ListAsync(
        ServiceRequestStatus? status, CancellationToken cancellationToken)
    {
        var query = context.ServiceRequests.AsNoTracking();
        if (status is { } filter)
        {
            query = query.Where(r => r.Status == filter);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<ServiceRequest?> FindNextPendingAsync(CancellationToken cancellationToken) =>
        context.ServiceRequests
            .Where(r => r.Status == ServiceRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<ServiceRequest?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.ServiceRequests.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task UpdateAsync(ServiceRequest request, CancellationToken cancellationToken)
    {
        if (context.Entry(request).State == EntityState.Detached)
        {
            context.ServiceRequests.Update(request);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException sqlException
        && UniqueViolationErrorNumbers.Contains(sqlException.Number);
}
