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
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

            var factory = new ConnectionFactory() { HostName = "wscagents.westus.cloudapp.azure.com", UserName = "guest", Password = "guest", VirtualHost = "/" };
            var connection = factory.CreateConnection();

            var subscriber = new MessageSubscriber(connection);
            var publisher = new MessagePublisher(connection);

            handleRabbitMQMessage(subscriber, openedSockets);

            app.Use(WebSocketMiddleware(subscriber, publisher, openedSockets));
        }


        private Func<HttpContext, Func<Task>, Task> WebSocketMiddleware(MessageSubscriber subscriber, MessagePublisher publisher, List<WebSocket> sockets)
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
                        var buffer = new ArraySegment<Byte>(new Byte[4096]);
                        var received = await webSocket.ReceiveAsync(buffer, token);

                        switch (received.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                handleNewMessage(buffer, publisher);
                                break;
                        }
                    }
                    sockets.Remove(webSocket);
                }
                else
                {
                    await next();
                }
            });
        }

        private void handleNewMessage(ArraySegment<byte> buffer, MessagePublisher publisher)
        {
            var request = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            publisher.Emit(request);
        }

        private void handleRabbitMQMessage(MessageSubscriber subscriber, List<WebSocket> sockets)
        {
            subscriber.Subscribe((model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                sockets.ForEach(async ws => await ws.SendAsync(new ArraySegment<Byte>(ea.Body), WebSocketMessageType.Text, true, CancellationToken.None));
            });
        }
    }

}