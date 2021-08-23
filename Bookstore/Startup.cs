using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bookstore.Constants.Authorization;
using Bookstore.Models;
using Bookstore.Utilities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Bookstore
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            
            services.AddHttpContextAccessor();
            
            services.AddControllersWithViews();
            services
                .AddEntityFrameworkSqlite()
                .AddDbContext<BookmarksContext>((sp, options) =>
                options
                    .UseSqlite(_configuration.GetConnectionString("BookstoreContext"))
                    .UseInternalServiceProvider(sp));
            
            string schema = CookieAuthenticationDefaults.AuthenticationScheme; // defaults to "Cookies"
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policy.AdminOnly, policy => policy.RequireClaim("role", BookstoreRoles.Admin));
                options.AddPolicy(Policy.MemberOnly, policy => policy.RequireClaim("role", BookstoreRoles.Member));
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            services.AddHttpClient();

            services.AddAuthentication(schema)
                .AddCookie(schema, options =>
                {
                    options.LoginPath = "/account/login";
                    options.LogoutPath = "/account/logout";
                    options.AccessDeniedPath = "/account/denied";
                });
            
            services.AddScoped<BookstoreService>(sp =>
            {
                var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
                var dbContext = sp.GetService<BookmarksContext>();
                return new BookstoreService(httpContextAccessor?.HttpContext?.User, dbContext);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCookiePolicy(new CookiePolicyOptions() {MinimumSameSitePolicy = SameSiteMode.Strict});
            app.UseStatusCodePages();
            
            // app.UseStaticFiles(new StaticFileOptions
            // {
            //     FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "data/archive")),
            //     RequestPath = "/archive-static",
            //     ServeUnknownFileTypes = true
            // });
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}