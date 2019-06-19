using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace Greeter
{
  class Greeter
  {
    private static IConnection connection;

    static void Main(string[] args)
    {
      var config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

      Console.WriteLine("Greeter starting to listen");
      var factory = new ConnectionFactory() { HostName = config["rabbitmq:url"] };
      connection = factory.CreateConnection();
      using (var channel = connection.CreateModel())
      {
        channel.QueueDeclare(queue: "greeter", durable: false, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
          HandleMessage(ea);
        };
        channel.BasicConsume(queue: "greeter", autoAck: true, consumer: consumer);

        while (true)
          System.Threading.Thread.Sleep(50);
      }
    }

    private static void HandleMessage(BasicDeliverEventArgs ea)
    {
      var message = Encoding.UTF8.GetString(ea.Body);
      Console.WriteLine($" [x] Recieved '{message}', replying to {ea.BasicProperties.ReplyTo} id {ea.BasicProperties.CorrelationId}");
      SendReply(ea.BasicProperties.ReplyTo, ea.BasicProperties.CorrelationId, $" [x] Received {message}");
    }

    private static void SendReply(string replyTo, string correlationId, string reply)
    {
      using (var channel = connection.CreateModel())
      {
        channel.QueueDeclare(queue: replyTo, durable: false, exclusive: false, autoDelete: false, arguments: null);

        var body = Encoding.UTF8.GetBytes(reply);

        var props = channel.CreateBasicProperties();
        props.CorrelationId = correlationId;
        channel.BasicPublish(exchange: "", routingKey: replyTo, basicProperties: props, body: body);
        Console.WriteLine($" [x] Sent '{reply}'");
      }
    }
  }
}
