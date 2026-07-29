using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OperationsHub.Domain.Entities;
using OperationsHub.Infrastructure.Identity;

namespace OperationsHub.Infrastructure.Persistence;

public sealed class OperationsHubDbContext : IdentityDbContext<ApplicationUser>
{
    public OperationsHubDbContext(DbContextOptions<OperationsHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<RequestType> RequestTypes => Set<RequestType>();

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    public DbSet<RequestAssignment> RequestAssignments => Set<RequestAssignment>();

    public DbSet<RequestComment> RequestComments => Set<RequestComment>();

    public DbSet<RequestStatusHistory> RequestStatusHistories => Set<RequestStatusHistory>();

    public DbSet<WorkLog> WorkLogs => Set<WorkLog>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(OperationsHubDbContext).Assembly);
        DevelopmentSeedData.Apply(builder);
    }
}
