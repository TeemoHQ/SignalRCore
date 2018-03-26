using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Server.Hub;

namespace Server
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
            services.AddMvc();
            services.AddSignalR(options => { options.KeepAliveInterval = TimeSpan.FromSeconds(5); });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseSignalR(config =>
            {
                config.MapHub<TestHub>("/signalr", options =>
                {
                    options.Transports = TransportType.WebSockets;
                    options.WebSockets.CloseTimeout = TimeSpan.FromMilliseconds(1000);
                });
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            SaveRecord();
        }

        private void SaveRecord()
        {
            TestHub.Srr.Ip = GetIp();
            RedisHelper.ConnectionString = "192.168.50.233:6379,192.168.50.221:6379,192.168.50.234:6379";
            RedisHelper rh = new RedisHelper();
            Task.Run(() =>
            {
                while (true)
                {
                    rh.SetSort(TestHub.Srr.Ip, JsonConvert.SerializeObject(TestHub.Srr), double.Parse(DateTime.Now.ToString("HHmmss")));
                    Thread.Sleep(30000);
                }
            });
        }

        public static string GetIp()
        {
            var str = string.Empty;
            string name = Dns.GetHostName();
            IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
            foreach (IPAddress ipa in ipadrlist)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {
                    str = ipa.ToString();
                    break;
                }
            }
            return string.IsNullOrEmpty(str) ? Guid.NewGuid().ToString() : str;
        }
    }
}
