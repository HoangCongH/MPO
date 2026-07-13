using Microsoft.EntityFrameworkCore;

namespace MPO_Web_Prj.Data;

public class MesDbContext : DbContext
{
    public MesDbContext(DbContextOptions<MesDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
