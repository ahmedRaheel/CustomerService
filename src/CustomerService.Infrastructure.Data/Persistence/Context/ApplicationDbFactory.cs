using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CustomerService.Infrastructure.Data.Persistence.Context;
public sealed class ApplicationDbFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=CustomerServiceDb;Trusted_Connection=True;MultipleActiveResultSets=true").Options;
        return new ApplicationDbContext(options);
    }
}