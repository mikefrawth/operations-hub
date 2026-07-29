using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationsHub.Domain.Entities;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Infrastructure.Persistence.Configurations;

public sealed class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistory> builder)
    {
        builder.ToTable("request_status_history");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).HasColumnName("id");
        builder.Property(history => history.ServiceRequestId).HasColumnName("service_request_id").IsRequired();
        builder.Property(history => history.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(history => history.ChangedById).HasColumnName("changed_by_id").HasMaxLength(255).IsRequired();
        builder.Property(history => history.ChangedAtUtc).HasColumnName("changed_at_utc").HasPrecision(6).IsRequired();
        builder.HasIndex(history => new { history.ServiceRequestId, history.ChangedAtUtc });
        builder.HasOne<ServiceRequest>().WithMany().HasForeignKey(history => history.ServiceRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(history => history.ChangedById).OnDelete(DeleteBehavior.Restrict);
    }
}
