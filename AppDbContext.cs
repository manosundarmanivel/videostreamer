using Microsoft.EntityFrameworkCore;
using TrainingVideoAPI.Models; // Replace with correct namespace


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TrainingVideo> TrainingVideos { get; set; }
}
