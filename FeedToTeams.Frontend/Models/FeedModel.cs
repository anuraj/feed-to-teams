namespace FeedToTeams.Frontend.Models;

public class FeedModel
{
    public int Id { get; set; }
    public string? RSSFeedURL { get; set; }
    public string? TeamId { get; set; }
    public string? TeamName { get; set; }
    public string? ChannelId { get; set; }
    public string? ChannelName { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? ChannelEndpoint { get; set; }
    public PublishedFeedModel? PublishedFeedModel { get; set; }
}
