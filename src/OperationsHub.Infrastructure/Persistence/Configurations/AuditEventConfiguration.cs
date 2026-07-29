using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationsHub.Domain.Entities;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(auditEvent => auditEvent.Id);
        builder.Property(auditEvent => auditEvent.Id).HasColumnName("id");
        builder.Property(auditEvent => auditEvent.ServiceRequestId).HasColumnName("service_request_id");
        builder.Property(auditEvent => auditEvent.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        builder.Property(auditEvent => auditEvent.ActorId).HasColumnName("actor_id").HasMaxLength(255).IsRequired();
        builder.Property(auditEvent => auditEvent.Details).HasColumnName("details").HasMaxLength(4_000);
        builder.Property(auditEvent => auditEvent.OccurredAtUtc).HasColumnName("occurred_at_utc").HasPrecision(6).IsRequired();
        builder.HasIndex(auditEvent => new { auditEvent.ServiceRequestId, auditEvent.OccurredAtUtc });
        builder.HasOne<ServiceRequest>().WithMany().HasForeignKey(auditEvent => auditEvent.ServiceRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(auditEvent => auditEvent.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
