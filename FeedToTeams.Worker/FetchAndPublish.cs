using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

using FeedToTeams.Worker.Utilities;

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders;

namespace FeedToTeams.Worker
{
    public class FetchAndPublish
    {
        private readonly IMsalTokenCacheProvider _msalTokenCacheProvider;
        private readonly IConfiguration _configuration;
        private readonly IMsalAccountActivityStore _msalAccountActivityStore;
        private readonly FeedToTeamsDbContext _feedToTeamsDbContext;

        public FetchAndPublish(IMsalTokenCacheProvider msalTokenCacheProvider, IConfiguration configuration,
            IMsalAccountActivityStore msalAccountActivityStore, FeedToTeamsDbContext feedToTeamsDbContext)
        {
            _msalTokenCacheProvider = msalTokenCacheProvider;
            _configuration = configuration;
            _msalAccountActivityStore = msalAccountActivityStore;
            _feedToTeamsDbContext = feedToTeamsDbContext;
        }

        [FunctionName("FetchAndPublish")]
        public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo timerInfo, ILogger log)
        {
            var scopes = _configuration["Scopes"].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToArray();
            var someTimeAgo = DateTime.Now.AddSeconds(-30);
            var accountsToAcquireToken = await _msalAccountActivityStore.GetMsalAccountActivitesSince(someTimeAgo);
            if (accountsToAcquireToken == null || accountsToAcquireToken.Count() == 0)
            {
                log.LogWarning($"No accounts returned");
            }
            else
            {
                var app = GetConfidentialClientApplication();
                foreach (var account in accountsToAcquireToken)
                {
                    var hydratedAccount = new MsalAccount
                    {
                        HomeAccountId = new AccountId(
                            account.AccountIdentifier,
                            account.AccountObjectId,
                            account.AccountTenantId)
                    };
                    try
                    {
                        var result = await app.AcquireTokenSilent(scopes, hydratedAccount).ExecuteAsync();
                        var feeds = _feedToTeamsDbContext.Feeds.Where(f => f.CreatedBy == account.AccountObjectId).ToList();

                        foreach (var feed in feeds)
                        {
                            using var reader = XmlReader.Create(feed.RSSFeedURL);
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
                                            Value = $"https://www.facebook.com/sharer.php?u={feedItem.Links.FirstOrDefault().Uri.ToString()}&t={feedItem.Title.Text}"
                                        },
                                        new Button
                                        {
                                            Type = "OpenUrl",
                                            Title = "Share To Twitter",
                                            Text = "Share To Twitter",
                                            DisplayText = "Share To Twitter",
                                            Value = $"https://twitter.com/intent/tweet?url={feedItem.Links.FirstOrDefault().Uri.ToString()}&text={feedItem.Title.Text}"
                                        },
                                        new Button
                                        {
                                            Type = "OpenUrl",
                                            Title = "Share To LinkedIn",
                                            Text = "Share To LinkedIn",
                                            DisplayText = "Share To LinkedIn",
                                            Value = $"https://www.linkedin.com/shareArticle?mini=true&url={feedItem.Links.FirstOrDefault().Uri.ToString()}&title={feedItem.Title.Text}&summary={feedItem.Summary.Text}"
                                        },
                                        new Button
                                        {
                                            Type = "OpenUrl",
                                            Title = "Share To WhatsApp",
                                            Text = "Share To WhatsApp",
                                            DisplayText = "Share To WhatsApp",
                                            Value = $"https://wa.me/?text={feedItem.Title.Text} - {feedItem.Links.FirstOrDefault().Uri.ToString()}"
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

                                    var graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
                                    {
                                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", result.AccessToken);
                                        return Task.FromResult(0);
                                    }));

                                    await graphServiceClient.Teams[feed.TeamId].Channels[feed.ChannelId].Messages
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
                        }
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        await _msalAccountActivityStore.HandleIntegratedTokenAcquisitionFailure(account);
                        log.LogError(ex, "User interaction is required.");
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Error occurred in the Run method.");
                    }
                }
            }
        }

        private IConfidentialClientApplication GetConfidentialClientApplication()
        {
            var app = ConfidentialClientApplicationBuilder.Create(_configuration["ClientId"])
                .WithClientSecret(_configuration["ClientSecret"])
                .Build();

            var msalCache = _msalTokenCacheProvider;

            msalCache.Initialize(app.UserTokenCache);

            return app;
        }
    }
}
