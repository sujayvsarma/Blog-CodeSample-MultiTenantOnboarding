using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using MultiTenantSample.AzureAd;

namespace MultiTenantSample
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
                        options.ResponseType = OpenIdConnectResponseType.Code;

                        foreach (string item in CONSTANTS.DEFAULT_AUTH_SCOPES)
                        {
                            options.Scope.Add(item);
                        }


                        options.TokenValidationParameters.IssuerValidator = MultiTenantIssuerValidator.GetIssuerValidator(options.Authority).Validate;
                        options.TokenValidationParameters.ValidateIssuer = true;

                        options.TokenValidationParameters.NameClaimType = "preferred_username";
                    }
                );

            ////////////////////////////////////////////////////////////////////////////////////////
            /// 
            /// ^^^^^^ Azure AD authZ configuration complete. Whew! :-)
            /// 
            ////////////////////////////////////////////////////////////////////////////////////////

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
        }


    }
}
