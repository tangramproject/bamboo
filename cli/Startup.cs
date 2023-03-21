// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using Cli.Commands.Common;
using McMaster.Extensions.CommandLineUtils;
using BAMWallet.Helper;
using BAMWallet.Model;
using BAMWallet.Rpc.Formatters;
using BAMWallet.Services;

namespace Cli
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<NetworkSettings>()
                .Bind(configuration.GetSection("NetworkSettings"));
            services.AddOptions<TimingSettings>()
                .Bind(configuration.GetSection("Timing"));

            services.AddHttpContextAccessor();
            services.AddSingleton(Log.Logger);
            services.AddSingleton<ISafeguardDownloadingFlagProvider, SafeguardDownloadingFlagProvider>();
            services.AddSingleton<ICommandReceiver, CommandReceiver>();
            services.AddSingleton<ICommandService, CommandInvoker>();
            services.AddSingleton<IHostedService, CommandInvoker>();
            services.AddSingleton<SafeguardService>();
            services.AddSingleton<IConsole>(PhysicalConsole.Singleton);

            services.AddMvcCore()
                .AddApiExplorer()
                .AddBinaryFormatter();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = "Attribution-NonCommercial-NoDerivatives 4.0 International",
                        Url = new Uri("https://raw.githubusercontent.com/tangramproject/tangram/master/LICENSE")
                    },
                    Title = "Bamboo Rest API",
                    Version = Util.GetAssemblyVersion(),
                    Description = "Bamboo Wallet Service.",
                    TermsOfService = new Uri("https://tangram.network/policy/"),
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Email = "",
                    }
                });

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging();

            var pathBase = configuration["PATH_BASE"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"{(!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty)}/swagger/v1/swagger.json", "BAMWalletRest.API V1");
                c.OAuthClientId("walletrestswaggerui");
                c.OAuthAppName("Bamboo Wallet Rest Swagger UI");
            });

            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
