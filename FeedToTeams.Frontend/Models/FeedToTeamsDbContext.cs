using Microsoft.EntityFrameworkCore;

namespace FeedToTeams.Frontend.Models
{
    public class FeedToTeamsDbContext : DbContext
    {
        public FeedToTeamsDbContext(DbContextOptions options) : base(options)
        {
        }

        protected FeedToTeamsDbContext()
        {
        }
        public DbSet<FeedModel>? Feeds { get; set; } = null!;
        public DbSet<MsalAccountActivityModel>? MsalAccountActivities { get; set; } = null!;
        public DbSet<PublishedFeedModel>? PublishedFeeds { get; set; } = null!;
    }
}
