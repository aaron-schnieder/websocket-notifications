using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationsApi.Notifications;
using NotificationsApi.Persistence;

namespace NotificationsApi
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            // Create SQLite DB if it doesn't exist
            using (var client = new NotificationsContext())
            {
                client.Database.EnsureCreated();
            }
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

#if DEBUG
            // Create an open CORS policy ** DON'T PUT THIS IN PRODUCTION **
            services.AddCors(o => o.AddPolicy("AllAllPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));
#endif

            // Configure the SQLite DB
            services.AddEntityFrameworkSqlite().AddDbContext<NotificationsContext>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

#if DEBUG
            // enable CORS
            app.UseCors("AllAllPolicy");
#endif

            app.UseMvc();

            /* WebSockets init options */
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4096
            };

            /* Register the use of web sockets in this app */
            app.UseWebSockets(webSocketOptions);

            /* Use custom aspnet core middleware to instantiate the web sockets */
            app.UseMiddleware<WebSocketsMiddleware>();
        }
    }
}
