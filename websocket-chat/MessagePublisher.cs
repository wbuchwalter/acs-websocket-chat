using System;
using System.Text;
using RabbitMQ.Client;

namespace websocket_chat
{
    public class MessagePublisher
    {
        private IConnection _connection;
        private IModel _channel;

        public MessagePublisher(IConnection connection)
        {
            _connection = connection;
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: "chat", type: "fanout");
        }

        public void Emit(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(exchange: "chat",
                                    routingKey: "",
                                    basicProperties: null,
                                    body: body);
        }
    }
}