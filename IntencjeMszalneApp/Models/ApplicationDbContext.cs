using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Intention> Intentions { get; set; }
    public DbSet<Mass> Masses { get; set; }
    public DbSet<User> Users { get; set; }
}