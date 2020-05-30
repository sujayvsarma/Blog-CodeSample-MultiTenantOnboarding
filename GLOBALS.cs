using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace SujaySarma.Cms
{
#nullable disable

    /// <summary>
    /// Globally accessible stuff
    /// </summary>
    public static class GLOBALS
    {
        /// <summary>
        /// Reference to the webhost environment. This will not be NULL, it is set up in 
        /// Startup.cs > Startup constructor
        /// </summary>
        public static IWebHostEnvironment WebHostEnvironment { get; set; }

        /// <summary>
        /// Reference to the environment configuration. This will not be NULL, it is set up in 
        /// Startup.cs > Startup constructor
        /// </summary>
        public static IConfiguration Configuration { get; set; }

    }
#nullable restore


    public static class CONSTANTS
    {

        public static string SHARE_NAME = "sujaycms";

        public static string GLOBAL_ADMINISTRATOR_ROLE_ID = "62e90394-69f5-4237-9190-012177145e10";

        public static string GRAPHAPI_SCOPE_DEFAULT = "https://graph.microsoft.com/.default";

        /// <summary>
        /// Default scopes for Graph API calls
        /// </summary>
        public static string[] GRAPHAPI_SCOPES_LIST = new string[] {
            "https://graph.microsoft.com/User.Read",
            "https://graph.microsoft.com/User.Read.All",
            "https://graph.microsoft.com/User.ReadBasic.All",
            "https://graph.microsoft.com/Group.Read.All",
            "https://graph.microsoft.com/Group.ReadWrite.All"
        };


        public static string[] DEFAULT_AUTH_SCOPES = new string[]
        {
            "https://graph.microsoft.com/User.Read"
        };

        /// <summary>
        /// Default scopes for AzureRM calls
        /// </summary>
        public static string[] AZURERM_SCOPE = new string[] { "https://management.core.windows.net/user_impersonation" };

        /// <summary>
        /// Scopes for admin consent presented during onboarding workflow
        /// </summary>
        public static string[] ONBOARDING_ADMIN_CONSENT_SCOPES_LIST = new string[]
        {
            "https://graph.microsoft.com/Directory.AccessAsUser.All",
            "https://graph.microsoft.com/Directory.Read.All",            
            "https://graph.microsoft.com/Group.Read.All",
            "https://graph.microsoft.com/Group.ReadWrite.All",
            "https://graph.microsoft.com/Organization.Read.All",
            "https://graph.microsoft.com/User.Read.All",
            "https://management.core.windows.net/user_impersonation"
        };


        /// <summary>
        /// The wellknown tenant Id for the AzureAD "common" & "consumers" endpoint
        /// </summary>
        public static System.Guid AZUREAD_TENANTID_WELLKNOWN_COMMON = System.Guid.Parse("9188040d-6c67-4c5b-b112-36a304b66dad");
    }


}
