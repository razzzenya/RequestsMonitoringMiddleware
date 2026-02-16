using Microsoft.EntityFrameworkCore;
using RequestMonitoring.Library.Enitites.Domain;

namespace RequestMonitoring.Library.Context;

public class DomainListsContext(DbContextOptions<DomainListsContext> options) : DbContext(options)
{
    public required DbSet<Domain> Domains { get; set; }
    public required DbSet<DomainStatusType> DomainStatusTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DomainStatusType>().HasData(
            new DomainStatusType { Id = 1, Name = "Allowed" },
            new DomainStatusType { Id = 2, Name = "Greylisted" },
            new DomainStatusType { Id = 3, Name = "Unauthorized" }
        );
    }
}
