using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReverseProxyApplication;
using VueCliMiddleware;

namespace SpaServer
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
            services
                .AddOptions()
                .AddSpaStaticFiles(configuration =>
                {
                    configuration.RootPath = "ClientApp/dist/spa";
                });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();

            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseRouting();
            
            app.UseMiddleware<ReverseProxyMiddleware>();
            
            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapToVueCliProxy(
            //        "{*path}",
            //        new SpaOptions { SourcePath = "ClientApp/dist/spa" },
            //        npmScript: (System.Diagnostics.Debugger.IsAttached) ? "serve" : null,
            //        regex: "Compiled successfully",
            //        forceKill: true,
		          //  wsl: false // Set to true if you are using WSL on windows. For other operating systems it will be ignored
            //    );
            //});

            app.UseSpa(spa =>{
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501
                spa.Options.SourcePath = "ClientApp";
                if (env.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer("http://pigeonspaserver.app");
                }
            });

        }
    }
}
