using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.AspNetCore.Authorization;
using FeedToTeams.Frontend.ViewModels;
using Microsoft.Identity.Web;
using System.Security.Claims;
using FeedToTeams.Frontend.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FeedToTeams.Frontend.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly GraphServiceClient _graphServiceClient;
    private readonly FeedToTeamsDbContext _feedToTeamsDbContext;

    public HomeController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient,
        FeedToTeamsDbContext feedToTeamsDbContext)
    {
        _logger = logger;
        _graphServiceClient = graphServiceClient;
        _feedToTeamsDbContext = feedToTeamsDbContext;
    }

    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> GetJoinedTeams()
    {
        var teamsResponse = await _graphServiceClient.Me.JoinedTeams.Request().GetAsync();
        var teams = teamsResponse.Select(team => new
        {
            Name = team.DisplayName,
            Description = team.Description,
            Id = team.Id
        });

        return Json(teams.ToList());
    }

    [Authorize]
    public async Task<IActionResult> GetChannelsById(string id)
    {
        var channelsResponse = await _graphServiceClient.Teams[id].Channels.Request().GetAsync();
        var channels = channelsResponse.Select(channel => new
        {
            Name = channel.DisplayName,
            Description = channel.Description,
            Id = channel.Id
        });

        return Json(channels.ToList());
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateFeed(Feed feed)
    {
        var objectidentifier = User.GetObjectId();
        var feedModel = new FeedModel
        {
            ChannelId = feed.ChannelId,
            ChannelName = feed.ChannelName,
            RSSFeedURL = feed.RSSFeedURL,
            TeamId = feed.TeamId,
            TeamName = feed.TeamName,
            CreatedBy = objectidentifier,
            CreatedOn = DateTime.UtcNow,
            IsEnabled = true
        };

        await _feedToTeamsDbContext.Feeds!.AddAsync(feedModel);
        await _feedToTeamsDbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFeedById), new { id = feedModel.Id }, feedModel);
    }

    [Authorize]
    public async Task<IActionResult> GetFeeds()
    {
        var objectidentifier = User.GetObjectId();
        var feeds = await _feedToTeamsDbContext.Feeds!
            .AsNoTracking()
            .Where(feed => feed.CreatedBy == objectidentifier)
            .OrderByDescending(feed => feed.CreatedOn)
            .ToListAsync();

        return Ok(feeds);
    }

    [Authorize]
    public async Task<IActionResult> GetFeedById(int id)
    {
        var feed = await _feedToTeamsDbContext.Feeds!.FindAsync(id);
        if (feed == null)
        {
            return NotFound();
        }

        return Ok(feed);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SendFeedToTeamsChannel(int id)
    {
        var feed = await _feedToTeamsDbContext.Feeds!.FindAsync(id);
        if (feed == null)
        {
            return NotFound();
        }

        using var reader = XmlReader.Create(feed!.RSSFeedURL!);
        var rssFeed = SyndicationFeed.Load(reader);
        var feedItem = rssFeed.Items.FirstOrDefault();
        if (feedItem != null)
        {
            var latestPublishedUrl = feedItem.Links.FirstOrDefault().Uri.ToString();
            var publishedFeed = _feedToTeamsDbContext.PublishedFeeds
                .FirstOrDefault(f => f.FeedModelId == feed.Id && f.LatestPublishedUrl == latestPublishedUrl);
            if (publishedFeed == null)
            {
                var card = new Card
                {
                    Title = feedItem.Title.Text,
                    Text = feedItem.Summary.Text,
                    Buttons = new List<Button>
                    {
                        new Button
                        {
                            Type = "OpenUrl",
                            Title = "View in browser",
                            Text = "View in browser",
                            DisplayText = "View in browser",
                            Value = feedItem.Links.FirstOrDefault().Uri.ToString()
                        },
                        new Button
                        {
                            Type = "OpenUrl",
                            Title = "Share To Facebook",
                            Text = "Share To Facebook",
                            DisplayText = "Share To Facebook",
                            Value = $"https://www.facebook.com/sharer.php?u={feedItem.Links.FirstOrDefault().Uri}&t={feedItem.Title.Text}"
                        },
                        new Button
                        {
                            Type = "OpenUrl",
                            Title = "Share To Twitter",
                            Text = "Share To Twitter",
                            DisplayText = "Share To Twitter",
                            Value = $"https://twitter.com/intent/tweet?url={feedItem.Links.FirstOrDefault().Uri}&text={feedItem.Title.Text}"
                        },
                        new Button
                        {
                            Type = "OpenUrl",
                            Title = "Share To LinkedIn",
                            Text = "Share To LinkedIn",
                            DisplayText = "Share To LinkedIn",
                            Value = $"https://www.linkedin.com/shareArticle?mini=true&url={feedItem.Links.FirstOrDefault().Uri}&title={feedItem.Title.Text}&summary={feedItem.Summary.Text}"
                        },
                        new Button
                        {
                            Type = "OpenUrl",
                            Title = "Share To WhatsApp",
                            Text = "Share To WhatsApp",
                            DisplayText = "Share To WhatsApp",
                            Value = $"https://wa.me/?text={feedItem.Title.Text} - {feedItem.Links.FirstOrDefault().Uri}"
                        }
                    }
                };
                var requestBody = new ChatMessage
                {
                    Subject = null,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = "<attachment id=\"74d20c7f34aa4a7fb74e2b30004247c5\"></attachment>",
                    },
                    Attachments = new List<ChatMessageAttachment>
                    {
                        new ChatMessageAttachment
                        {
                            Id = "74d20c7f34aa4a7fb74e2b30004247c5",
                            ContentType = "application/vnd.microsoft.card.thumbnail",
                            ContentUrl = null,
                            Content = JsonSerializer.Serialize(card),
                            Name = null,
                            ThumbnailUrl = null,
                        },
                    },
                };

                await _graphServiceClient.Teams[feed.TeamId].Channels[feed.ChannelId].Messages
                    .Request()
                    .AddAsync(requestBody);

                _feedToTeamsDbContext.PublishedFeeds.Add(new PublishedFeedModel
                {
                    LatestPublishedUrl = latestPublishedUrl,
                    CreatedOn = DateTime.UtcNow,
                    FeedModel = feed,
                    FeedModelId = feed.Id,
                });

                _feedToTeamsDbContext.SaveChanges();
            }
        }

        return Ok();
    }
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
