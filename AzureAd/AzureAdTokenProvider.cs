using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;

using MultiTenantSample.Data;
using MultiTenantSample.MvcServices;

using System;
using System.Threading.Tasks;

namespace MultiTenantSample.AzureAd
{
    /// <summary>
    /// Engine that retrieves and manages Azure AD tokens
    /// </summary>
    public class AzureAdTokenProvider : IAzureAdTokenProvider
    {
        /// <summary>
        /// Initializes a token cache (which can be a user token cache or an app token cache)
        /// </summary>
        /// <param name="tokenCache">Token cache for which to initialize the serialization</param>
        public Task InitializeAsync(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(OnBeforeAccess);
            tokenCache.SetAfterAccess(OnAfterAccess);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Remove cached entry for the given account
        /// </summary>
        /// <param name="account">Account to remove</param>
        public void RemoveAccount(IAccount account)
        {
            if (account.HomeAccountId != null)
            {
                _tableStorageService.GetDataSource<AzureAdToken>()
                    .Delete(
                        new AzureAdToken()
                        {
                            TenantId = account.HomeAccountId.TenantId,
                            UserName = account.HomeAccountId.ObjectId
                        }
                    );
            }
        }

        private void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            (string? pk, string? rk) = GetCacheKey(args.IsApplicationCache);
            if ((pk == null) || (rk == null))
            {
                return;
            }

            byte[] data;

            AzureAdToken? tokenItem = _tableStorageService.GetDataSource<AzureAdToken>()
                .SelectSingleObject<AzureAdToken>(partitionKey: pk, rowKey: rk);

            if (tokenItem != null)
            {
                data = System.Text.Encoding.UTF8.GetBytes(tokenItem.Token!);
                args.TokenCache.DeserializeMsalV3(data, true);
            }
        }

        private void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                (string? pk, string? rk) = GetCacheKey(args.IsApplicationCache);
                if ((pk == null) || (rk == null))
                {
                    return;
                }

                var ds = _tableStorageService.GetDataSource<AzureAdToken>();

                //The delete() throws a 404 if the row was not found
                try
                {
                    ds.Delete(
                            new AzureAdToken()
                            {
                                TenantId = pk,
                                UserName = rk
                            }
                        );
                }
                catch { }

                ds.Insert(
                        new AzureAdToken()
                        {
                            TenantId = pk,
                            UserName = rk,
                            Token = System.Text.Encoding.UTF8.GetString(args.TokenCache.SerializeMsalV3())
                        }
                    );
            }
        }


        private (string? tenantId, string? userId) GetCacheKey(bool isAppCache) =>
            (isAppCache ? (tenantId: Guid.Empty.ToString("n"), userId: string.Empty) : (tenantId: _httpContext.HttpContext.User.GetTenantId(), userId: _httpContext.HttpContext.User.GetObjectId()));

        public AzureAdTokenProvider(IAzureTableStorageService tableStorageService, IHttpContextAccessor httpContextAccessor)
        {
            _tableStorageService = tableStorageService;
            _httpContext = httpContextAccessor;
        }


        private readonly IAzureTableStorageService _tableStorageService;
        private readonly IHttpContextAccessor _httpContext;
    }
}
