using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security;

namespace MultiTenantSample.AzureAd
{
    /// <summary>
    /// A validator that validates tokens received from the Azure AD authentication endpoint
    /// </summary>
    public class MultiTenantIssuerValidator
    {

        /// <summary>
        /// Validate the issuer
        /// </summary>
        /// <param name="actualIssuer">The issuer to validate</param>
        /// <param name="securityToken">The security token that was received from the auth endpoint</param>
        /// <param name="validationParameters">Token validation parameters</param>
        /// <returns></returns>
        public string Validate(string actualIssuer, SecurityToken securityToken, TokenValidationParameters validationParameters)
        {
            if (string.IsNullOrWhiteSpace(actualIssuer)) { throw new ArgumentNullException(nameof(actualIssuer)); }
            if (securityToken == null) { throw new ArgumentNullException(nameof(securityToken)); }
            if (validationParameters == null) { throw new ArgumentNullException(nameof(validationParameters)); }

            string tenantId = string.Empty;
            if (securityToken is JwtSecurityToken jwt)
            {
                if (jwt.Payload.TryGetValue("tid", out object? tid))
                {
                    tenantId = (string)tid;
                }
                else if (securityToken is JsonWebToken jwebToken)
                {
                    tenantId = jwebToken.GetPayloadValue<string>("tid");
                }
            }

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new SecurityTokenInvalidIssuerException("Neither 'tid' not 'tenantid' are present in the claim token.");
            }

            if (!Uri.TryCreate(actualIssuer, UriKind.Absolute, out Uri? actualIssuerUri))
            {
                throw new ArgumentException(nameof(actualIssuer));
            }

            if (validationParameters.ValidIssuers != null)
            {
                foreach (string issuerTemplate in validationParameters.ValidIssuers)
                {
                    if (IsIssuerValid(issuerTemplate, tenantId, actualIssuerUri))
                    {
                        return actualIssuer;
                    }
                }
            }

            if (IsIssuerValid(validationParameters.ValidIssuer, tenantId, actualIssuerUri))
            {
                return actualIssuer;
            }

            throw new SecurityTokenInvalidIssuerException($"Issuer '{actualIssuer}' did not match any specified valid issuer.");


            bool IsIssuerValid(string template, string tenantId, Uri actualIssuer)
            {
                if (string.IsNullOrWhiteSpace(template) || (issuerAliases == null))
                {
                    return false;
                }

                if (Uri.TryCreate(template.Replace("{tenantid}", tenantId), UriKind.Absolute, out Uri? replacedIssuer))
                {
                    return issuerAliases.Contains(replacedIssuer.Authority) &&
                            issuerAliases.Contains(actualIssuer.Authority) &&
                                HasValidTenantIdInLocalPath(tenantId, replacedIssuer) &&
                                    HasValidTenantIdInLocalPath(tenantId, actualIssuer);
                }

                return false;


                static bool HasValidTenantIdInLocalPath(string tenantId, Uri uri)
                {
                    string trimmedPath = uri.LocalPath.Trim('/');
                    return ((trimmedPath == tenantId) || (trimmedPath == $"{tenantId}/v2.0"));
                }
            }
        }

        /// <summary>
        /// Used only during configuration Get an instance of the issuer validator
        /// </summary>
        /// <param name="authority">Authority string to retrieve the MultiTenantIssuerValidator for</param>
        /// <returns>MultiTenantIssuerValidator instance</returns>
        public static MultiTenantIssuerValidator GetIssuerValidator(string authority)
        {
            if (string.IsNullOrWhiteSpace(authority))
            {
                throw new ArgumentNullException(nameof(authority));
            }

            MultiTenantIssuerValidator? validator = null;
            try
            {
                validator = cachedValidators.GetOrAdd(
                        authority,
                        (authority) =>
                        {
                            IssuerMetadata metadata = configurationManager.GetConfigurationAsync().Result;

                            string authorityHostName = AzureAdPrimaryAuthority;
                            if (Uri.TryCreate(authority, UriKind.Absolute, out Uri? authorityUri))
                            {
                                authorityHostName = authorityUri.Authority;
                            }

                            return new MultiTenantIssuerValidator()
                            {
                                issuerAliases = new HashSet<string>(
                                        metadata.Metadata
                                            .Where(m => m.Aliases.Any(a => string.Equals(a, authorityHostName, StringComparison.OrdinalIgnoreCase)))
                                                .SelectMany(m => m.Aliases)
                                                    .Distinct(),
                                        StringComparer.OrdinalIgnoreCase
                                    )
                            };
                        }
                    );
            }
            catch
            {
                cachedValidators.TryRemove(authority, out _);
            }

            return validator ?? throw new SecurityException($"Unable to retrieve validator for '{authority}' authority.");
        }


        static MultiTenantIssuerValidator()
        {
            cachedValidators = new ConcurrentDictionary<string, MultiTenantIssuerValidator>();
            configurationManager = new ConfigurationManager<IssuerMetadata>(AzureAdIssuerMetadataUrl, new AzureAdConfigurationRetriever());
        }

        private ISet<string>? issuerAliases = null;

        private static readonly ConcurrentDictionary<string, MultiTenantIssuerValidator> cachedValidators;
        private static readonly ConfigurationManager<IssuerMetadata> configurationManager;

        private const string AzureAdIssuerMetadataUrl = "https://login.microsoftonline.com/common/discovery/instance?authorization_endpoint=https://login.microsoftonline.com/common/oauth2/v2.0/authorize&api-version=1.1";
        private const string AzureAdPrimaryAuthority = "login.microsoftonline.com";
    }
}
