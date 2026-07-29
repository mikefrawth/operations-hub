using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationsHub.Domain.Entities;

namespace OperationsHub.Infrastructure.Persistence.Configurations;

public sealed class RequestTypeConfiguration : IEntityTypeConfiguration<RequestType>
{
    public void Configure(EntityTypeBuilder<RequestType> builder)
    {
        builder.ToTable("request_types");
        builder.HasKey(requestType => requestType.Id);
        builder.Property(requestType => requestType.Id).HasColumnName("id");
        builder.Property(requestType => requestType.Name).HasColumnName("name").HasMaxLength(RequestType.NameMaximumLength).IsRequired();
        builder.Property(requestType => requestType.Description).HasColumnName("description").HasMaxLength(RequestType.DescriptionMaximumLength);
        builder.Property(requestType => requestType.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(requestType => requestType.CreatedAtUtc).HasColumnName("created_at_utc").HasPrecision(6).IsRequired();
        builder.HasIndex(requestType => requestType.Name).IsUnique();
    }
}
