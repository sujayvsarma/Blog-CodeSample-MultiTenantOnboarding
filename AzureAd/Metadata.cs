using Newtonsoft.Json;

using System.Collections.Generic;

namespace MultiTenantSample.AzureAd
{

    /// <summary>
    /// Model child class to hold alias information parsed from the Azure AD issuer endpoint.
    /// </summary>
    internal class Metadata
    {
        /// <summary>
        /// Preferred alias
        /// </summary>
        [JsonProperty(PropertyName = "preferred_network")]
        public string PreferredNetwork { get; set; } = string.Empty;

        /// <summary>
        /// Preferred alias to cache tokens emitted by one of the aliases (to avoid
        /// SSO islands)
        /// </summary>
        [JsonProperty(PropertyName = "preferred_cache")]
        public string PreferredCache { get; set; } = string.Empty;

        /// <summary>
        /// Aliases of issuer URLs which are equivalent
        /// </summary>
        [JsonProperty(PropertyName = "aliases")]
        public List<string> Aliases { get; set; } = new List<string>();
    }
}
