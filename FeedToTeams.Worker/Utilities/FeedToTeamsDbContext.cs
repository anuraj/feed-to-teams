using Microsoft.EntityFrameworkCore;

namespace FeedToTeams.Worker.Utilities;

public class FeedToTeamsDbContext : DbContext
{
    public FeedToTeamsDbContext(DbContextOptions<FeedToTeamsDbContext> options) : base(options)
    {
    }

    public DbSet<MsalAccountActivity> MsalAccountActivities { get; set; }
    public DbSet<FeedModel> Feeds { get; set; }
    public DbSet<PublishedFeedModel> PublishedFeeds { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}
