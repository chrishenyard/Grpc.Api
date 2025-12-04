using Grpc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Grpc.Data.DbContexts;

public class GrpcDbContext(DbContextOptions<GrpcDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>().HasQueryFilter(j => j.IsActive == true);
    }

    public DbSet<ApiClient> ApiClients { get; set; }
    public DbSet<ApiClientSecret> ApiClientSecrets { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<ApiClientGroup> ApiClientGroups { get; set; }
    public DbSet<ApiGroup> ApiGroups { get; set; }
}
