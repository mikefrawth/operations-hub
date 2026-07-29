using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationsHub.Domain.Entities;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Infrastructure.Persistence.Configurations;

public sealed class RequestAssignmentConfiguration : IEntityTypeConfiguration<RequestAssignment>
{
    public void Configure(EntityTypeBuilder<RequestAssignment> builder)
    {
        builder.ToTable("request_assignments");
        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id).HasColumnName("id");
        builder.Property(assignment => assignment.ServiceRequestId).HasColumnName("service_request_id").IsRequired();
        builder.Property(assignment => assignment.AssigneeId).HasColumnName("assignee_id").HasMaxLength(255).IsRequired();
        builder.Property(assignment => assignment.AssignedById).HasColumnName("assigned_by_id").HasMaxLength(255).IsRequired();
        builder.Property(assignment => assignment.AssignedAtUtc).HasColumnName("assigned_at_utc").HasPrecision(6).IsRequired();
        builder.HasIndex(assignment => new { assignment.ServiceRequestId, assignment.AssignedAtUtc });
        builder.HasOne<ServiceRequest>().WithMany().HasForeignKey(assignment => assignment.ServiceRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(assignment => assignment.AssigneeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(assignment => assignment.AssignedById).OnDelete(DeleteBehavior.Restrict);
    }
}
