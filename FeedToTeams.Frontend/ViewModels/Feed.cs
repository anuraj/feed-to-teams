using Microsoft.AspNetCore.Mvc.Rendering;

using System.ComponentModel.DataAnnotations;

namespace FeedToTeams.Frontend.ViewModels;

public class Feed
{
    [Required, Url]
    public string? RSSFeedURL { get; set; }
    [Required]
    public string? TeamId { get; set; }
    public string? TeamName { get; set; }
    [Required]
    public string? ChannelId { get; set; }
    public string? ChannelName { get; set; }
    public SelectList? Teams { get; set; }
}