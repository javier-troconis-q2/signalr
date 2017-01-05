using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.AspNetCore.Http.Extensions;



using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Configuration;
using Microsoft.AspNetCore.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


using signalr.server.Hubs;

using Microsoft.WindowsAzure.Storage.Blob;

namespace signalr.server
{

    

    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            app.UseWebSockets();

            app.Map("/x", x => x.UseMiddleware<X>());
            app.Map("/y", x => x.UseMiddleware<Y>());

            //app.UseMiddleware<ReverseProxyMiddleware>();
            //app.Map("/_signalr", x =>
            //{
            //    x.UseStaticFiles();
            //    x.RunSignalR();
            //});

            var publishMessage = new PublishMessageAsync(async (tenantId, topic, message) =>
            {
                var connectionManager = app.ApplicationServices.GetService<IConnectionManager>();
                var hub = connectionManager.GetHubContext<EventHub>();
                IClientProxy proxy = hub.Clients.Group(tenantId + "-" + topic);
                await proxy.Invoke(topic, message);
            });
            var tenants = new[] { "1" };
            var topics = new[] { "product-created" };
            var rnd = new Random();
            new Timer(async delegate
            {
                var tenant = tenants[rnd.Next(tenants.Length)];
                var topic = topics[rnd.Next(topics.Length)];
                var message = $"{tenant}-{topic}-{DateTime.Now.ToLongTimeString()}";
                await publishMessage(tenant, topic, message);
            }, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
         
            services.AddSignalR(x =>
            {
                x.Transports = new TransportOptions { EnabledTransports = TransportType.WebSockets };
                x.Hubs.EnableDetailedErrors = true;
                x.Hubs.PipelineModules.Add(new HubAuthotizationModule());
            });
        }
    }

    public class X
    {
        private readonly RequestDelegate _next;

        public X(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var clientSocket = await context.WebSockets.AcceptWebSocketAsync();
            var serverSocket = new ClientWebSocket();
            await serverSocket.ConnectAsync(new Uri("ws://localhost:8104/y"), CancellationToken.None);

            Task.Run(async () =>
            {
                while (true)
                {
                    await ReverseProxyMiddleware.ForwardMessage(clientSocket, serverSocket);
                }
            });

            Task.Run(async () =>
            {
                while (true)
                {
                    await ReverseProxyMiddleware.ForwardMessage(serverSocket, clientSocket);
                }
            });

            while (true)
            {
                
            }
        }
    }

    public class Y
    {
        private readonly RequestDelegate _next;
        private static int _count;

        static Y()
        {
            new Timer(delegate { _count = _count + 1; }, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
        }

        public Y(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var socket = await context.WebSockets.AcceptWebSocketAsync();

            Task.Run(async () =>
            {
                while (true)
                {
                    var buffer = new byte[1024];
                    await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    Console.WriteLine(_count.ToString());
                }
            });

            Task.Run(async () =>
            {
                while (true)
                {
                    await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(_count.ToString())), WebSocketMessageType.Text, true, CancellationToken.None);
                    await Task.Delay(1000);
                }
            });

            while (true)
            {

            }
        }
    }
}