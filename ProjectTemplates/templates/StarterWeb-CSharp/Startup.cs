using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Builder;
#if (IndividualLocalAuth)
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
#endif
using Microsoft.AspNetCore.Hosting;
#if (RequiresHttps)
using Microsoft.AspNetCore.HttpsPolicy;
#endif
#if (OrganizationalAuth)
using Microsoft.AspNetCore.Mvc.Authorization;
#endif
#if (IndividualLocalAuth)
using Microsoft.EntityFrameworkCore;
using Company.WebApplication1.Data;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if(MultiOrgAuth)
using Microsoft.IdentityModel.Tokens;
#endif
#if (GenerateGraph)
using Microsoft.Graph;
#endif

namespace Company.WebApplication1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
#if (IndividualLocalAuth)
            services.AddDbContext<ApplicationDbContext>(options =>
#if (UseLocalDB)
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
#else
                options.UseSqlite(
                    Configuration.GetConnectionString("DefaultConnection")));
#endif
            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
#elif (OrganizationalAuth)
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"))
#if (GenerateApiOrGraph)
                        .EnableTokenAcquisitionToCallDownstreamApi()
                        .AddInMemoryTokenCaches();
#else
                    ;
#endif
#if (GenerateApi)
            services.AddDownstreamWebApiService(Configuration);
#endif
#if (GenerateGraph)
            services.AddMicrosoftGraph(Configuration.GetValue<string>("CalledApi:CalledApiScopes")?.Split(' '),
                                       Configuration.GetValue<string>("CalledApi:CalledApiUrl"));
#endif
#elif (IndividualB2CAuth)
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAdB2C"))
#if (GenerateApi)
                        .EnableTokenAcquisitionToCallDownstreamApi()
                        .AddInMemoryTokenCaches();

            services.AddDownstreamWebApiService(Configuration);
#else
                    ;
#endif
#endif
#if (OrganizationalAuth)

            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });
#else
            services.AddControllersWithViews();
#endif
#if (OrganizationalAuth || IndividualB2CAuth)
           services.AddRazorPages()
                   .AddMicrosoftIdentityUI();
#endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
#if (IndividualLocalAuth)
                app.UseDatabaseErrorPage();
#endif
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
#if (RequiresHttps)
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
#else
            }
#endif
            app.UseStaticFiles();

            app.UseRouting();

#if (OrganizationalAuth || IndividualAuth)
            app.UseAuthentication();
#endif
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
#if (OrganizationalAuth || IndividualAuth)
                endpoints.MapRazorPages();
#endif
            });
        }
    }
}
