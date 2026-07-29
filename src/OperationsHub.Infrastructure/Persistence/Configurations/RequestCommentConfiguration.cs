using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationsHub.Domain.Entities;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Infrastructure.Persistence.Configurations;

public sealed class RequestCommentConfiguration : IEntityTypeConfiguration<RequestComment>
{
    public void Configure(EntityTypeBuilder<RequestComment> builder)
    {
        builder.ToTable("request_comments");
        builder.HasKey(comment => comment.Id);
        builder.Property(comment => comment.Id).HasColumnName("id");
        builder.Property(comment => comment.ServiceRequestId).HasColumnName("service_request_id").IsRequired();
        builder.Property(comment => comment.AuthorId).HasColumnName("author_id").HasMaxLength(255).IsRequired();
        builder.Property(comment => comment.Body).HasColumnName("body").HasMaxLength(4_000).IsRequired();
        builder.Property(comment => comment.CreatedAtUtc).HasColumnName("created_at_utc").HasPrecision(6).IsRequired();
        builder.HasIndex(comment => new { comment.ServiceRequestId, comment.CreatedAtUtc });
        builder.HasOne<ServiceRequest>().WithMany().HasForeignKey(comment => comment.ServiceRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(comment => comment.AuthorId).OnDelete(DeleteBehavior.Restrict);
    }
}
