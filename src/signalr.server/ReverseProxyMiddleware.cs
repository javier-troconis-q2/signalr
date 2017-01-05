using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;


namespace signalr.server
{
    public class ReverseProxyMiddleware
    {
        private readonly ConcurrentDictionary<HostString, HttpClient> _clients = new ConcurrentDictionary<HostString, HttpClient>();

        private readonly RequestDelegate _next;

        public ReverseProxyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public static async Task ForwardMessage(WebSocket @from, WebSocket to)
        {
            using (var stream = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    var buffer = new byte[1024];
                    result = await @from.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    stream.Write(buffer, 0, buffer.Length);
                } while (!result.EndOfMessage);
                await to.SendAsync(new ArraySegment<byte>(stream.ToArray()), result.MessageType, result.EndOfMessage, CancellationToken.None);
            }
        }

        public static void CopyHeaders(IHeaderDictionary @from, ClientWebSocketOptions to, params string[] keys)
        {
            foreach (var key in keys)
            {
                to.SetRequestHeader(key, string.Join("; ", @from[key].ToArray()));
            }
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Value.Contains("/_signalr"))
            {
                await _next(context);
                return;
            }

            context.Request.Path = new PathString(context.Request.Path.Value.Replace("/signalr", "/_signalr"));

            if (context.WebSockets.IsWebSocketRequest)
            {
            //    //var clientSocket = await context.WebSockets.AcceptWebSocketAsync();

            //    //var serverSocket = new ClientWebSocket();

            //    //CopyHeaders(context.Request.Headers, serverSocket.Options,
            //    //    "Accept-Encoding",
            //    //    "Accept-Language",
            //    //    "Cache-Control",
            //    //    //"Connection",
            //    //    "Cookie",
            //    //    //"Host",
            //    //    "Origin",
            //    //    "Pragma",
            //    //    "Sec-WebSocket-Extensions",
            //    //    "Sec-WebSocket-Key",
            //    //    "Sec-WebSocket-Version",
            //    //    "Upgrade" //,
            //    //    //"User-Agent"
            //    //    );

            //    //await serverSocket.ConnectAsync(new Uri(context.Request.GetEncodedUrl().Replace("http://", "ws://")), CancellationToken.None);

            //    ////await clientSocket.ReceiveAsync(new ArraySegment<byte>(new byte[0]), CancellationToken.None);

            //    //Task.Run(async () =>
            //    //{
            //    //    while (true)
            //    //    {
            //    //        await ForwardMessage(clientSocket, serverSocket);
            //    //    }
            //    //});

            //    //Task.Run(async () =>
            //    //{
            //    //    while (true)
            //    //    {
            //    //        await ForwardMessage(serverSocket, clientSocket);
            //    //    }
            //    //});

            //    while (true)
            //    {
            //    }
                await _next(context);
                return;
            }

            HostString host = context.Request.Host;
            HttpClient client = _clients.GetOrAdd(host, new HttpClient());

            var requestMessage = new HttpRequestMessage();

            string requestMethod = context.Request.Method;
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                requestMessage.Content = new StreamContent(context.Request.Body);
            }

            foreach (KeyValuePair<string, StringValues> header in context.Request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            requestMessage.RequestUri = new Uri(context.Request.GetEncodedUrl());
            requestMessage.Method = new HttpMethod(context.Request.Method);

            using (HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
            {
                context.Response.StatusCode = (int)responseMessage.StatusCode;

                foreach (KeyValuePair<string, IEnumerable<string>> header in responseMessage.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (KeyValuePair<string, IEnumerable<string>> header in responseMessage.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                // SendAsync removes chunking from the response. This removes the header so it doesn't expect from chunked response.
                context.Response.Headers.Remove("transfer-encoding");
                await responseMessage.Content.CopyToAsync(context.Response.Body);
            }
        }
    }
}
