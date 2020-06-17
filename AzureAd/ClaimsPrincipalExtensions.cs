using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace MultiTenantSample.AzureAd
{
    /// <summary>
    /// Extensions to do various things with ClaimsPrincipal instances
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {

        /// <summary>
        /// Get an account identifer for an Msal.net account from a ClaimsPrincipal
        /// </summary>
        /// <param name="principal">Claims principal</param>
        /// <returns>String corresponding to the Microsoft.Identity.Client.AccountId.Identifier value. NULL if either ObjectId or TenantId 
        /// are not populated in the claims</returns>
        public static string? GetMsalAccountId(this ClaimsPrincipal principal)
        {
            string? oid = principal.GetObjectId();
            string? tid = principal.GetTenantId();

            if (string.IsNullOrWhiteSpace(oid) || string.IsNullOrWhiteSpace(tid))
            {
                return null;
            }

            return $"{oid}.{tid}";
        }

        /// <summary>
        /// Get the Oid or ObjectIdentifier value from the claims
        /// </summary>
        /// <param name="principal">Claims principal</param>
        /// <returns>Oid value or NULL</returns>
        public static string? GetObjectId(this ClaimsPrincipal principal)
            => principal.Claims.GetFirstClaimValue("oid", "http://schemas.microsoft.com/identity/claims/objectidentifier");

        /// <summary>
        /// Get the Tid or TenantId value from the claims
        /// </summary>
        /// <param name="principal">Claims principal</param>
        /// <returns>Tid value or NULL</returns>
        public static string? GetTenantId(this ClaimsPrincipal principal)
            => principal.Claims.GetFirstClaimValue("tid", "http://schemas.microsoft.com/identity/claims/tenantid");

        /// <summary>
        /// Gets the login Id used before
        /// </summary>
        /// <param name="principal">Claims principal</param>
        /// <returns>Login value or NULL</returns>
        public static string? GetLoginUserName(this ClaimsPrincipal principal)
            => principal.Claims.GetFirstClaimValue("preferred_username", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "name");

        /// <summary>
        /// Get a list of all group Ids returned in the user's claims
        /// </summary>
        /// <param name="principal">Context user principal</param>
        /// <returns>List of group ids</returns>
        public static IList<string> GetUserGroups(this ClaimsPrincipal principal)
        {
            IList<string> groups = new List<string>();

            foreach (Claim claim in principal.Claims)
            {
                if (claim.Type == "groups")
                {
                    try
                    {
                        groups.Add(claim.Value);
                    }
                    catch
                    {
                        //its probably a group, not a role
                    }
                }
            }

            return groups;
        }



        /// <summary>
        /// Get the first claim value
        /// </summary>
        /// <param name="claims">Collection of claims</param>
        /// <param name="claimNames">Array of names</param>
        /// <returns>Returns the value of the claim matching the first string in the array of claim names</returns>
        public static string? GetFirstClaimValue(this IEnumerable<Claim> claims, params string[] claimNames)
        {
            foreach (Claim claim in claims)
            {
                for (int i = 0; i < claimNames.Length; i++)
                {
                    if (claim.Type.Equals(claimNames[i], StringComparison.InvariantCultureIgnoreCase))
                    {
                        return claim.Value;
                    }
                }
            }

            return null;
        }
    }
}
