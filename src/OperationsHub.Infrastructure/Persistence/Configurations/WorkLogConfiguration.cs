using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationsHub.Domain.Entities;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Infrastructure.Persistence.Configurations;

public sealed class WorkLogConfiguration : IEntityTypeConfiguration<WorkLog>
{
    public void Configure(EntityTypeBuilder<WorkLog> builder)
    {
        builder.ToTable("work_logs");
        builder.HasKey(workLog => workLog.Id);
        builder.Property(workLog => workLog.Id).HasColumnName("id");
        builder.Property(workLog => workLog.ServiceRequestId).HasColumnName("service_request_id").IsRequired();
        builder.Property(workLog => workLog.AuthorId).HasColumnName("author_id").HasMaxLength(255).IsRequired();
        builder.Property(workLog => workLog.Hours).HasColumnName("hours").HasPrecision(5, 2).IsRequired();
        builder.Property(workLog => workLog.Note).HasColumnName("note").HasMaxLength(1_000);
        builder.Property(workLog => workLog.CreatedAtUtc).HasColumnName("created_at_utc").HasPrecision(6).IsRequired();
        builder.HasIndex(workLog => new { workLog.ServiceRequestId, workLog.CreatedAtUtc });
        builder.HasOne<ServiceRequest>().WithMany().HasForeignKey(workLog => workLog.ServiceRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(workLog => workLog.AuthorId).OnDelete(DeleteBehavior.Restrict);
    }
}
