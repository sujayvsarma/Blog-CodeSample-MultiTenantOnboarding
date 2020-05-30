using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using SujaySarma.Cms.Data.ConfigurationDB;
using SujaySarma.Cms.MvcServices.AzureAd;
using SujaySarma.Cms.MvcServices.Data;
using SujaySarma.Cms.MvcServices.Email;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SujaySarma.Cms
{
    public class Startup
    {
        // constructor
        public Startup(IWebHostEnvironment env, IConfiguration config)
        {
            GLOBALS.WebHostEnvironment = env;
            GLOBALS.Configuration = config;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseExceptionHandler(
                        (error) =>
                        {
                            error.Run(context =>
                            {
                                IExceptionHandlerFeature exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                                Exception exception = exceptionFeature.Error;
                                if (exception is AggregateException ae)
                                {
                                    exception = ae.InnerException!;
                                }

                                if (exception is UnauthorizedAccessException uae)
                                {
                                    // we need to login again
                                    return context.ChallengeAsync(
                                            AzureADDefaults.AuthenticationScheme,
                                            new AuthenticationProperties() 
                                            { 
                                                RedirectUri = context.Request.GetEncodedUrl()
                                            }
                                        );
                                }

                                if (exception is MsalUiRequiredException msal)
                                {
                                    if ((msal.Classification == UiRequiredExceptionClassification.ConsentRequired) || msal.Message.Contains("AADSTS65001"))
                                    {
                                        // The user or administrator has not consented to use the application... 
                                        // need to send an interactive consent challenge

                                        AuthenticationProperties properties = new AuthenticationProperties();
                                        properties.SetParameter(OpenIdConnectParameterNames.Prompt, "consent");

                                        // the scopes required were populated into Data[] by the code that (re)threw this exception
                                        properties.Items.Add("tid", context.User.GetTenantId());
                                        if (msal.Data.Contains("scopes"))
                                        {
                                            properties.Items.Add("scope", (string)msal.Data["scopes"]!);
                                        }

                                        properties.RedirectUri = context.Request.GetEncodedUrl();

                                        return context.ChallengeAsync(
                                                AzureADDefaults.AuthenticationScheme,
                                                properties
                                            );
                                    }

                                    // if it is not something we want to deal with, dont do anything to the response here.
                                    // It will fall through to the SendErrorMessageText() blocks below.
                                }

                                // Since it was here an exception we could not handle above, make a note of it 
                                // in our trace logs (aka AppInsights) system:
                                System.Diagnostics.Trace.WriteLine($"Unhandled exception: {exceptionFeature.Error.Message}\r\n{exceptionFeature.Error}");

                                return SendErrorMessageText((GLOBALS.WebHostEnvironment.IsDevelopment() ? exceptionFeature.Error.ToString() : "A server error occurred."));

                                // Takes care of sending out the error information in a consistent manner
                                async Task SendErrorMessageText(string text)
                                {
                                    context.Response.StatusCode = 500;
                                    context.Response.ContentType = "text/plain";

                                    await context.Response.WriteAsync(exceptionFeature.Error.ToString());
                                }
                            });
                        }
                    );

            app.UseHsts();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseCookiePolicy();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}"
                    );

                // Required for the AzureAD Auth Module
                endpoints.MapRazorPages();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None;
                options.ConsentCookie = new Microsoft.AspNetCore.Http.CookieBuilder()
                {
                    IsEssential = true
                };
            });

            services.AddDistributedMemoryCache();
            services.AddSession(
                    options =>
                    {
                        options.IdleTimeout = new System.TimeSpan(0, 30, 0);
                        options.Cookie.IsEssential = true;
                    }
                );


            ////////////////////////////////////////////////////////////////////////////////////////
            /// 
            /// All of this is for AzureAd authentication
            /// 
            ////////////////////////////////////////////////////////////////////////////////////////

            IConfigurationSection azureAdSection = GLOBALS.Configuration.GetSection("AzureAd");

            services
                .AddAuthentication(AzureADDefaults.AuthenticationScheme)
                    .AddAzureAD(
                        options => azureAdSection.Bind(options)
                    );

            services.Configure<AzureADOptions>(
                    options => azureAdSection.Bind(options)
                );

            services.Configure<ConfidentialClientApplicationOptions>(
                    options => azureAdSection.Bind(options)
                );

            services.Configure<OpenIdConnectOptions>(
                    AzureADDefaults.OpenIdScheme,
                    (options) =>
                    {
                        options.Authority += "/v2.0/";
                        if (options.Authority.Contains("/common/"))
                        {
                            options.Authority = options.Authority.Replace("/common/", "/organizations/");
                        }
                        options.ResponseType = OpenIdConnectResponseType.CodeIdToken;

                        foreach(string item in CONSTANTS.DEFAULT_AUTH_SCOPES)
                        {
                            options.Scope.Add(item);
                        }

                        options.TokenValidationParameters.IssuerValidator = AadIssuerValidator.GetIssuerValidator(options.Authority).Validate;
                        options.TokenValidationParameters.ValidateIssuer = false;

                        options.TokenValidationParameters.NameClaimType = "preferred_username";

                        // OnRedirectToIdentityProvider
                        options.Events.OnRedirectToIdentityProvider = OpenIdConnect_OnRedirectToIdentityProvider;

                        // OnAuthorizationCodeReceived
                        previousOnAuthorizationCodeReceived = options.Events.OnAuthorizationCodeReceived;
                        options.Events.OnAuthorizationCodeReceived = OpenIdConnect_OnAuthorizationCodeReceived;

                        // OnRedirectToIdentityProviderForSignOut
                        options.Events.OnRedirectToIdentityProviderForSignOut = OpenIdConnect_OnRedirectToIdentityProviderForSignOut;

                        // OnTokenValidated - Validate the tenant & associate if required
                        options.Events.OnTokenValidated = OpenIdConnect_OnTokenValidated;

                        // We should probably also handle OnAuthenticationFailed
                    }
                );

            ////////////////////////////////////////////////////////////////////////////////////////
            /// 
            /// ^^^^^^ Azure AD authZ configuration complete. Whew! :-)
            /// 
            ////////////////////////////////////////////////////////////////////////////////////////

            services.AddMemoryCache();              // <---- Is this required ??
            services.AddHttpContextAccessor();

            // refresher: These are created just once. So these should not contain global values that would be 
            //            specific to a request or a user.

            services.AddSingleton<ITenantService, TenantService>();
            services.AddSingleton<IAzureTableStorageService, AzureTablesService>();
            services.AddSingleton<IEmailService, EmailService>();
            services.AddSingleton<IMsalTokenCacheProvider, MsalAzureTableTokenCacheProvider>(); //MsalInMemoryTokenCacheProvider
            services.AddSingleton<IPrincipalResolutionService, PrincipalResolutionService>();

            // refresher: These are created per-request. So these can load request/user specific info in 
            //            their constructors

            services.AddScoped<IAdTokenAcquisitionService, AdTokenAcquisitionService>();           

            services.AddControllersWithViews(
                    options =>
                    {
                        AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                                        .RequireAuthenticatedUser()
                                            .Build();

                        options.Filters.Add(new AuthorizeFilter(policy));
                    }
                ).AddNewtonsoftJson();

            // Required for the AzureAD Auth Module
            services.AddRazorPages();
            services.AddApplicationInsightsTelemetry(GLOBALS.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
        }

        // Event handler for OpenIdConnectOptions
        private Task OpenIdConnect_OnRedirectToIdentityProvider(RedirectContext context)
        {
            if (context.ProtocolMessage.Prompt == "consent")
            {
                // consent UX, ensure scopes
                if (context.Properties.Items.ContainsKey("scope"))
                {
                    context.ProtocolMessage.Scope = context.Properties.Items["scope"];
                }                
            }
            else
            {                
                context.ProtocolMessage.Prompt = "select_account";
            }

            if (context.Properties.Items.ContainsKey("claims"))
            {
                context.ProtocolMessage.SetParameter("claims", context.Properties.Items["claims"]);
            }

            return Task.FromResult(0);
        }

        // Event handler for OpenIdConnectOptions
        private async Task OpenIdConnect_OnAuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        {
            IAdTokenAcquisitionService tokenService = context.HttpContext.RequestServices.GetRequiredService<IAdTokenAcquisitionService>();

            await tokenService.GetAccessTokenUsingAuthorizationCode(context);
            if (previousOnAuthorizationCodeReceived != null)
            {
                await previousOnAuthorizationCodeReceived(context);
            }
        }
        private System.Func<AuthorizationCodeReceivedContext, Task>? previousOnAuthorizationCodeReceived = null;

        // Event handler for OpenIdConnectOptions
        private async Task OpenIdConnect_OnRedirectToIdentityProviderForSignOut(RedirectContext context)
            => await context.HttpContext.RequestServices.GetRequiredService<IAdTokenAcquisitionService>().RemoveAccount(context);

        // Event handler for OpenIdConnectOptions
        private Task OpenIdConnect_OnTokenValidated(TokenValidatedContext context)
        {
            string? userObjectId = context.SecurityToken.Claims.GetFirstClaimValue("oid", "http://schemas.microsoft.com/identity/claims/objectidentifier");
            string? azureTenantId = context.SecurityToken.Claims.GetFirstClaimValue("tid", "http://schemas.microsoft.com/identity/claims/tenantid");

            if ((userObjectId == null) || (azureTenantId == null) || (!Guid.TryParse(azureTenantId, out Guid tenantId)))
            {
                throw new ApplicationException("Unable to fetch critical claims from authentication provider.");
            }

            // Never call context.HandleResponse() when sending the user to the onboarding/association URI. 
            // doing so would not populate the claims that we would need in the user association workflow!

            IAzureTableStorageService tableService = context.HttpContext.RequestServices.GetRequiredService<IAzureTableStorageService>();
            ITenantService tenantService = context.HttpContext.RequestServices.GetRequiredService<ITenantService>();
            
            if (tenantId == CONSTANTS.AZUREAD_TENANTID_WELLKNOWN_COMMON)
            {
                IList<UserAssociation> associations = tableService.GetDataSource<UserAssociation>().Select<UserAssociation>(rowKey: userObjectId).ToList();
                if (associations.Count == 0)
                {
                    throw new ApplicationException($"You logged on using a 'Microsoft Account ID'. You must log on using an account configured in your Azure Tenant. Contact Us if you do not know how.");
                }

                UserAssociation defaultTenant = associations.First(a => a.IsDefault);
                
                context.Response.Redirect($"https://login.microsoftonline.com/{defaultTenant.TenantId:d}/v2.0/authorize?client_id={context.Options.ClientId}&redirect_uri={context.Properties.RedirectUri}");
                context.HandleResponse();
            }
            else
            {
                Tenant? tenant = tenantService.Get(tenantId);
                if (tenant == null)
                {
                    context.Properties.RedirectUri = "/onboard/request-consent";
                }
                else
                {
                    UserAssociation? association = tableService.GetDataSource<UserAssociation>().SelectSingleObject<UserAssociation>(partitionKey: azureTenantId, rowKey: userObjectId);
                    if (association == null)
                    {
                        tenantService.AssociateUser(tenantId, userObjectId, false);

                        context.Properties.RedirectUri = $"/onboard/consent-user/{tenant:d}";
                    }
                }
            }

            return Task.FromResult(0);
        }
    }
}
