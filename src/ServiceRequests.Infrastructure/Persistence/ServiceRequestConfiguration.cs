using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceRequests.Core;

namespace ServiceRequests.Infrastructure.Persistence;

internal sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.ExternalReference)
            .IsRequired()
            .HasMaxLength(ServiceRequest.ExternalReferenceMaxLength);

        builder.Property(r => r.CustomerId)
            .IsRequired()
            .HasMaxLength(ServiceRequest.CustomerIdMaxLength);

        builder.Property(r => r.ServiceCode)
            .IsRequired()
            .HasMaxLength(ServiceRequest.ServiceCodeMaxLength);

        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(ServiceRequest.DescriptionMaxLength);

        // Stored as text for readable rows and stable values if the enum is reordered.
        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.FailureReason)
            .HasMaxLength(ServiceRequest.FailureReasonMaxLength);

        // Final guard for idempotency under concurrent submissions (TD-06).
        builder.HasIndex(r => r.ExternalReference)
            .IsUnique()
            .HasDatabaseName("UX_ServiceRequests_ExternalReference");

        // Status filter in the API and Pending polling in the Worker.
        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_ServiceRequests_Status");
    }
}
