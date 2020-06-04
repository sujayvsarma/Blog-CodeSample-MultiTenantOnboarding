using Microsoft.IdentityModel.Protocols;

using Newtonsoft.Json;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultiTenantSample.AzureAd
{
    /// <summary>
    /// A class that implements IConfigurationRetriever and helps populate the issuer configuration & metadata 
    /// from the Azure AD Authentication endpoint.
    /// </summary>
    internal class AzureAdConfigurationRetriever : IConfigurationRetriever<IssuerMetadata>
    {
        /// <summary>
        /// Retrieves the configuration document and deserializes it
        /// </summary>
        /// <param name="address">Url where the document is to be downloaded from</param>
        /// <param name="retriever">Engine that retrieves the document. Injected by ASP.NET Core at runtime</param>
        /// <param name="cancellationToken">Async process cancellation token</param>
        /// <returns>Deserialized issuer metadata</returns>
        public async Task<IssuerMetadata> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(address)) { throw new ArgumentNullException(nameof(address)); }
            if (retriever == null) { throw new ArgumentNullException(nameof(retriever)); }

            return JsonConvert.DeserializeObject<IssuerMetadata>(await retriever.GetDocumentAsync(address, cancellationToken));
        }
    }
}
