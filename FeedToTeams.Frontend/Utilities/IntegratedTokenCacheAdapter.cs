using FeedToTeams.Frontend.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;

namespace FeedToTeams.Frontend.Utilities
{
    public class IntegratedTokenCacheAdapter : MsalDistributedTokenCacheAdapter
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MsalDistributedTokenCacheAdapter> _logger;

        public IntegratedTokenCacheAdapter(
            IServiceScopeFactory scopeFactory,
            IDistributedCache memoryCache,
            IOptions<MsalDistributedTokenCacheAdapterOptions> cacheOptions,
            ILogger<MsalDistributedTokenCacheAdapter> logger) : base(memoryCache, cacheOptions, logger)
        {
            this._scopeFactory = scopeFactory;
            this._logger = logger;
        }

        // Overriding OnBeforeWriteAsync to upsert the entity MsalAccountActivity
        // before MSAL writes an entry in the token cache
        protected override async Task OnBeforeWriteAsync(TokenCacheNotificationArgs args)
        {
            var accountActivity = new MsalAccountActivityModel(args.SuggestedCacheKey, args.Account);
            await UpsertActivity(accountActivity);

            _logger.LogInformation($"{args.SuggestedCacheKey}-{args.Account}");

            await Task.FromResult(base.OnBeforeWriteAsync(args));
        }

        // Call the upsert method of the class that implements IMsalAccountActivityStore
        private async Task<MsalAccountActivityModel> UpsertActivity(MsalAccountActivityModel accountActivity)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _integratedTokenCacheStore = scope.ServiceProvider.GetRequiredService<IMsalAccountActivityStore>();

                await _integratedTokenCacheStore.UpsertMsalAccountActivity(accountActivity);

                return accountActivity;
            }
        }
    }
}
