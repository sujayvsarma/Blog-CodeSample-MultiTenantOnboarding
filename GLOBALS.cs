using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace MultiTenantSample
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

}
