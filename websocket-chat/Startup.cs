using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace websocket_chat
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseDeveloperExceptionPage();

            var openedSockets = new List<WebSocket>();
            app.UseWebSockets();

            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("172.17.0.1:6379");
            ISubscriber subscriber = redis.GetSubscriber();           

            SubscribeMessage(subscriber, openedSockets);
            app.Use(WebSocketMiddleware(subscriber, openedSockets));
        }


        private Func<HttpContext, Func<Task>, Task> WebSocketMiddleware(ISubscriber subscriber, List<WebSocket> sockets)
        {
            return (async (HttpContext http, Func<Task> next) =>
            {
                int key = 0;
                if (http.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await http.WebSockets.AcceptWebSocketAsync();
                    sockets.Add(webSocket);
                    while (webSocket.State == WebSocketState.Open)
                    {
                        var token = CancellationToken.None;
                        var buffer = new ArraySegment<Byte>(new Byte[4092]);
                        var received = await webSocket.ReceiveAsync(buffer, token);

                        switch (received.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                var message = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, received.Count);
                                PublishMessage(subscriber, message);
                                break;
                        }
                    }
                    sockets.Remove(webSocket);
                }
                else
                {
                    await http.Response.WriteAsync("websocket-chat running.");
                }
            });
        }

        private void PublishMessage(ISubscriber subscriber, string message)
        {            
            Console.WriteLine(message);
            subscriber.Publish("chat", message);
        }

        private void SubscribeMessage(ISubscriber subscriber, List<WebSocket> sockets)
        {
            subscriber.Subscribe("chat", (chan, message) =>
            {
                sockets.ForEach(async ws => await ws.SendAsync(new ArraySegment<Byte>(Encoding.ASCII.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None));
            });
        }
    }

}