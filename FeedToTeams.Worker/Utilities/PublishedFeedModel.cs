using System;

namespace FeedToTeams.Worker.Utilities;

public class PublishedFeedModel
{
    public int Id { get; set; }
    public FeedModel FeedModel { get; set; }
    public int FeedModelId { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string LatestPublishedUrl { get; set; }
}