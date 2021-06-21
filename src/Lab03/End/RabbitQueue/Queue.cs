using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace RabbitQueue
{
  /// <summary>
  /// Helper methods to simplify use of RabbitMQ
  /// </summary>
  public class Queue : IDisposable
  {
    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly string serviceName;

    /// <summary>
    /// Create an instance of Queue
    /// </summary>
    /// <param name="hostName">Host name or URL for RabbitMQ service</param>
    /// <param name="serviceName">Name of the queue</param>
    public Queue(string hostName, string serviceName)
    {
      this.serviceName = serviceName;
      var factory = new ConnectionFactory() { HostName = hostName };
      connection = factory.CreateConnection();
      channel = connection.CreateModel();
    }

    /// <summary>
    /// Send a message to the queue
    /// </summary>
    /// <param name="destination">Name of the destination queue</param>
    /// <param name="correlationId">Unique correlation id</param>
    /// <param name="request">Request object (must be serializable to JSON)</param>
    public void SendMessage(string destination, string correlationId, object request)
    {
      SendMessage(serviceName, destination, correlationId, request);
    }

    /// <summary>
    /// Send a reply to a message
    /// </summary>
    /// <param name="destination">Name of the destination queue</param>
    /// <param name="correlationId">Unique correlation id</param>
    /// <param name="request">Request object (must be serializable to JSON)</param>
    public void SendReply(string destination, string correlationId, object request)
    {
      SendMessage(null, destination, correlationId, request);
    }

    private void SendMessage(string sender, string destination, string correlationId, object request)
    {
      var message = JsonConvert.SerializeObject(request);
      channel.QueueDeclare(queue: destination, durable: false, exclusive: false, autoDelete: false, arguments: null);

      var body = Encoding.UTF8.GetBytes(message);

      var props = channel.CreateBasicProperties();
      if (!string.IsNullOrWhiteSpace(sender))
        props.ReplyTo = sender;
      props.CorrelationId = correlationId;
      props.Type = request.GetType().Name;
      channel.BasicPublish(exchange: "", routingKey: destination, basicProperties: props, body: body);
    }

    /// <summary>
    /// Start listening for messages of a single type
    /// to arrive on the queue
    /// </summary>
    /// <typeparam name="M">Type of message</typeparam>
    /// <param name="handleMessage">Method to invoke for each message</param>
    public void StartListening<M>(Action<BasicDeliverEventArgs, M> handleMessage)
    {
      StartListening((ea, message) =>
      {
        var response = JsonConvert.DeserializeObject<M>(message);
        handleMessage?.Invoke(ea, response);
      });
    }

    /// <summary>
    /// Start listening for messages to arrive on the queue
    /// </summary>
    /// <param name="handleMessage">Method to invoke for each message</param>
    public void StartListening(Action<BasicDeliverEventArgs, string> handleMessage)
    {
      channel.QueueDeclare(queue: serviceName, durable: false, exclusive: false, autoDelete: false, arguments: null);

      var consumer = new EventingBasicConsumer(channel);
      consumer.Received += (model, ea) =>
      {
        var message = Encoding.UTF8.GetString(ea.Body.ToArray());
        handleMessage(ea, message);
      };
      channel.BasicConsume(queue: serviceName, autoAck: true, consumer: consumer);
    }

    /// <summary>
    /// Dispose the queue
    /// </summary>
    public void Dispose()
    {
      channel.Close();
      connection.Close();
    }
  }
}
