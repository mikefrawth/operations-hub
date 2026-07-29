using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationsHub.Domain.Entities;
using OperationsHub.Domain.Enums;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Infrastructure.Persistence.Configurations;

public sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("service_requests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).HasColumnName("id");
        builder.Property(request => request.RequestNumber).HasColumnName("request_number").HasMaxLength(32).IsRequired();
        builder.Property(request => request.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(request => request.Description).HasColumnName("description").HasMaxLength(4_000).IsRequired();
        builder.Property(request => request.RequesterId).HasColumnName("requester_id").HasMaxLength(255).IsRequired();
        builder.Property(request => request.RequestTypeId).HasColumnName("request_type_id").IsRequired();
        builder.Property(request => request.DepartmentId).HasColumnName("department_id");
        builder.Property(request => request.AssigneeId).HasColumnName("assignee_id").HasMaxLength(255);
        builder.Property(request => request.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(request => request.Priority).HasColumnName("priority").HasConversion<int>().IsRequired();
        builder.Property(request => request.CreatedAtUtc).HasColumnName("created_at_utc").HasPrecision(6).IsRequired();
        builder.Property(request => request.UpdatedAtUtc).HasColumnName("updated_at_utc").HasPrecision(6).IsRequired();
        builder.Property(request => request.Version).HasColumnName("version").IsRowVersion();

        builder.HasIndex(request => request.RequestNumber).IsUnique();
        builder.HasIndex(request => new { request.Status, request.CreatedAtUtc });
        builder.HasIndex(request => request.RequestTypeId);
        builder.HasIndex(request => request.RequesterId);
        builder.HasIndex(request => request.DepartmentId);
        builder.HasIndex(request => request.AssigneeId);
        builder.HasOne<RequestType>().WithMany().HasForeignKey(request => request.RequestTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(request => request.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(request => request.RequesterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(request => request.AssigneeId).OnDelete(DeleteBehavior.Restrict);
    }
}
