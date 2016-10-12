using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
           
            app.UseWebSockets();
            
            var wsd = new Dictionary<int, WebSocket>();
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("127.0.0.1:6379,abortConnect=false");
            ISubscriber sub = redis.GetSubscriber();
            sub.Subscribe("messages", (chan, message) => {
                Console.WriteLine((string)message);
            });

            sub.Publish("messages", "hello :) :)");
            
            app.Use(async (http, next) =>
            {
                
                if (http.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await http.WebSockets.AcceptWebSocketAsync();
                    int key;
                    if (wsd.Count == 0)
                    {
                        key = 1;
                    }
                    else
                    {
                        key = wsd.Keys.Max() + 1;
                    }
                    wsd.Add(key, webSocket);
                    while (webSocket.State == WebSocketState.Open)
                    {
                        var token = CancellationToken.None;
                        var buffer = new ArraySegment<Byte>(new Byte[4096]);
                        var received = await webSocket.ReceiveAsync(buffer, token);

                        switch (received.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                var request = Encoding.UTF8.GetString(buffer.Array,
                                                        buffer.Offset,
                                                        buffer.Count);

                                var type = WebSocketMessageType.Text;
                                var data = Encoding.UTF8.GetBytes("User " + key + ": " + request);
                                buffer = new ArraySegment<Byte>(data);
                                wsd.Values.ToList().ForEach(async ws => await ws.SendAsync(buffer, type, true, token));
                                break;
                        }
                    }
                    wsd.Remove(key);
                }
                else
                {
                    await next();
                }
            });
           
        }
    }
}