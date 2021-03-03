using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Ocelot.Administration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;


namespace GateWay
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
             .MinimumLevel.Debug()
             .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
             .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
             .MinimumLevel.Override("System", LogEventLevel.Warning)
             .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
             .Enrich.FromLogContext()
             // uncomment to write to Azure diagnostics stream
             //.WriteTo.File(
             //    @"D:\home\LogFiles\Application\identityserver.txt",
             //    fileSizeLimitBytes: 1_000_000,
             //    rollOnFileSizeLimit: true,
             //    shared: true,
             //    flushToDiskInterval: TimeSpan.FromSeconds(1))
             .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
             .CreateLogger();
            Log.Information("Starting host...");
            CreateHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateHostBuilder(string[] args) =>
           WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://*:4000")
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddJsonFile("ocelot.json") 
                        .AddEnvironmentVariables();
                })
             .ConfigureServices(s =>
             {
                 //Action<JwtBearerOptions> options = o =>
                 //{
                 //    o.Authority = identityServerRootUrl;
                 //    o.RequireHttpsMetadata = false;
                 //    o.TokenValidationParameters = new TokenValidationParameters
                 //    {
                 //        ValidateAudience = false,
                 //    };
                 //    // etc....
                 //};
                 var authenticationProviderKey = "IS";
                 Action<JwtBearerOptions> options = o =>
                 {
                     o.Authority = "http://localhost:5000";
                     o.RequireHttpsMetadata = false;
                     
                     //o.ClientId = "client";
                     //o.ClientSecret = new Secret("secret".Sha256());
                     //o.ResponseType = "code";

                     //options.Scope.Add("api1");
                     // etc
                 };

                 s.AddAuthentication()
                  .AddJwtBearer(authenticationProviderKey, options =>
                  {
                      options.Authority = "http://localhost:5000";
                      options.RequireHttpsMetadata = false;
                      options.TokenValidationParameters = new TokenValidationParameters
                      {
                          ValidateAudience = false
                      };
                  });
                 // adds an authorization policy to make sure the token is for scope 'api1'
                 s.AddAuthorization(options =>
                 {
                     options.AddPolicy("ApiScope", policy =>
                     {
                         policy.RequireAuthenticatedUser();
                         policy.RequireClaim("scope", "api1");
                     });
                 });
                 //.AddJwtBearer(authenticationProviderKey, options);

                 s.AddOcelot()
                // .AddAdministration("/administration", "secret")
                ;
                 s.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
             })
                .Configure(app =>
                {

                     
                    app.UseAuthentication();
                    
                    app.UseOcelot().Wait();
                });
    }
    public class FooAgger : IDefinedAggregator
    {
        public Task<DownstreamResponse> Aggregate(List<HttpContext> responses)
        {
            //foreach (var item in responses)
            //{
            //    item.
            //} 
            throw new NotImplementedException();
        }
    }
}
