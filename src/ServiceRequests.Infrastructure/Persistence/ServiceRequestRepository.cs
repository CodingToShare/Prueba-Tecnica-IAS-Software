using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ServiceRequests.Core;

namespace ServiceRequests.Infrastructure.Persistence;

internal sealed class ServiceRequestRepository(ServiceRequestsDbContext context) : IServiceRequestRepository
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

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException sqlException
        && UniqueViolationErrorNumbers.Contains(sqlException.Number);
}
