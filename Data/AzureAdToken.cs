using SujaySarma.Sdk.DataSources.AzureTables.Attributes;

namespace MultiTenantSample.Data
{
    /// <summary>
    /// Stores persistent sessions for Msal (Microsoft Identity Services - Azure AD) tokens
    /// </summary>
    [Table("AzureAdToken")]
    public class AzureAdToken
    {
        /// <summary>
        /// User's tenant Id
        /// </summary>
        [PartitionKey]
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Login Id of the user
        /// </summary>
        [RowKey]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// The serialized token
        /// </summary>
        [TableColumn("token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Default constructor
        /// </summary>
        public AzureAdToken() { }

    }
}
