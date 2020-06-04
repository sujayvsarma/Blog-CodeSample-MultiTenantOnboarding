using Newtonsoft.Json;

using System.Collections.Generic;

namespace MultiTenantSample.AzureAd
{
    /// <summary>
    /// Model class to hold information parsed from the Azure AD issuer endpoint
    /// </summary>
    internal class IssuerMetadata
    {
        /// <summary>
        /// Tenant discovery endpoint
        /// </summary>
        [JsonProperty(PropertyName = "tenant_discovery_endpoint")]
        public string TenantDiscoveryEndpoint { get; set; } = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

        /// <summary>
        /// API Version
        /// </summary>
        [JsonProperty(PropertyName = "api-version")]
        public string ApiVersion { get; set; } = "1.1";

        /// <summary>
        /// List of metadata associated with the endpoint
        /// </summary>
        [JsonProperty(PropertyName = "metadata")]
        public List<Metadata> Metadata { get; set; } = new List<Metadata>();
    }
}
