using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace websocket_chat
{
    class MessageSubscriber
    {

        private EventingBasicConsumer _consumer;
        public MessageSubscriber(IConnection connection)
        {
            var channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: "chat", type: "fanout");
            var queueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(queue: queueName,
                               exchange: "chat",
                               routingKey: "");
            _consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume(queue: queueName,
                                noAck: true,
                                consumer: _consumer);
        }

        public void Subscribe(Action<Object, BasicDeliverEventArgs> func)
        {
            _consumer.Received += (model, ea) =>
            {
                func(model, ea);
            };
        }
    }
}