using Microsoft.Identity.Client;

using System.Threading.Tasks;

namespace MultiTenantSample.AzureAd
{
    /// <summary>
    /// Interface for our token retrieval and management system.
    /// </summary>
    public interface IAzureAdTokenProvider
    {
        /// <summary>
        /// Initializes a token cache (which can be a user token cache or an app token cache)
        /// </summary>
        /// <param name="tokenCache">Token cache for which to initialize the serialization</param>
        Task InitializeAsync(ITokenCache tokenCache);

        /// <summary>
        /// Remove cached entry for the given account
        /// </summary>
        /// <param name="account">Account to remove</param>
        void RemoveAccount(IAccount account);
    }
}
