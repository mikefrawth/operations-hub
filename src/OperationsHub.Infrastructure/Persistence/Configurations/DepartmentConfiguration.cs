using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OperationsHub.Domain.Entities;

namespace OperationsHub.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(department => department.Id);
        builder.Property(department => department.Id).HasColumnName("id");
        builder.Property(department => department.Name).HasColumnName("name").HasMaxLength(Department.NameMaximumLength).IsRequired();
        builder.Property(department => department.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(department => department.CreatedAtUtc).HasColumnName("created_at_utc").HasPrecision(6).IsRequired();
        builder.HasIndex(department => department.Name).IsUnique();
    }
}
