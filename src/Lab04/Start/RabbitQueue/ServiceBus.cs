using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace RabbitQueue
{
  /// <summary>
  /// Service bus implementation that uses a
  /// publish/subscribe model based on a RabbitMQ
  /// exchange and subscriptions
  /// </summary>
  public class ServiceBus : IServiceBus
  {
    private IConnection connection;
    private IModel channel;
    private readonly string hostName;
    private readonly string exchangeName;

    /// <summary>
    /// Create an instance of ServiceBus
    /// </summary>
    /// <param name="hostName">Host name or URL for RabbitMQ service</param>
    /// <param name="busName">Name of the bus</param>
    public ServiceBus(string hostName, string busName)
    {
      this.hostName = hostName;
      exchangeName = busName;
    }

    private void Initialize()
    {
      if (channel == null)
      {
        var factory = new ConnectionFactory() { HostName = hostName };
        connection = factory.CreateConnection();
        channel = connection.CreateModel();
        channel.ExchangeDeclare(exchangeName, "direct");
      }
    }

    /// <summary>
    /// Publish a message to the service bus
    /// </summary>
    /// <param name="bindingKey">Binding key for the message</param>
    /// <param name="correlationId">Correlation id</param>
    /// <param name="message">Message body</param>
    public void Publish(string bindingKey, string correlationId, object message)
    {
      Initialize();
      var serialized = JsonConvert.SerializeObject(message);
      var body = Encoding.UTF8.GetBytes(serialized);

      var props = channel.CreateBasicProperties();
      props.CorrelationId = correlationId;
      props.ReplyTo = "";

      channel.BasicPublish(
        exchange: exchangeName,
        routingKey: bindingKey,
        basicProperties: props,
        body: body);
    }

    /// <summary>
    /// Start listening for messages of a single type
    /// to arrive on the bus
    /// </summary>
    /// <param name="subscriptionName">Name of subscription</param>
    /// <param name="bindingKeys">Binding keys to subscribe</param>
    /// <typeparam name="M">Type of message</typeparam>
    /// <param name="handleMessage">Method to invoke for each message</param>
    public void Subscribe<M>(string[] bindingKeys, Action<BasicDeliverEventArgs, M> handleMessage)
    {
      Subscribe(bindingKeys, (ea, message) =>
      {
        var response = JsonConvert.DeserializeObject<M>(message);
        handleMessage?.Invoke(ea, response);
      });
    }

    /// <summary>
    /// Start listening for messages of a single type
    /// and binding key to arrive on the bus
    /// </summary>
    /// <param name="bindingKey">Binding key to subscribe</param>
    /// <typeparam name="M">Type of message</typeparam>
    /// <param name="handleMessage">Method to invoke for each message</param>
    public void Subscribe<M>(string bindingKey, Action<BasicDeliverEventArgs, M> handleMessage)
    {
      Subscribe(new string[] { bindingKey }, (ea, message) =>
      {
        var response = JsonConvert.DeserializeObject<M>(message);
        handleMessage?.Invoke(ea, response);
      });
    }

    /// <summary>
    /// Start listening for messages of a single binding
    /// key to arrive on the bus
    /// </summary>
    /// <param name="bindingKey">Binding key to subscribe</param>
    /// <typeparam name="M">Type of message</typeparam>
    /// <param name="handleMessage">Method to invoke for each message</param>
    public void Subscribe(string bindingKey, Action<BasicDeliverEventArgs, string> handleMessage)
    {
      Subscribe(new string[] { bindingKey }, (ea, message) =>
      {
        handleMessage?.Invoke(ea, message);
      });
    }

    /// <summary>
    /// Start listening for messages to arrive on the bus
    /// </summary>
    /// <param name="bindingKeys">Binding keys to subscribe</param>
    /// <param name="handleMessage">Method to invoke for each message</param>
    public void Subscribe(string[] bindingKeys, Action<BasicDeliverEventArgs, string> handleMessage)
    {
      Initialize();
      var queueName = channel.QueueDeclare().QueueName;
      foreach (var item in bindingKeys)
      {
        channel.QueueBind(
          queue: queueName,
          exchange: exchangeName,
          routingKey: item);
      }

      var consumer = new EventingBasicConsumer(channel);
      consumer.Received += (model, ea) =>
      {
        var message = Encoding.UTF8.GetString(ea.Body);
        handleMessage(ea, message);
      };
      channel.BasicConsume(
        queue: queueName, 
        autoAck: true, 
        consumer: consumer);
    }

    /// <summary>
    /// Dispose the bus
    /// </summary>
    public void Dispose()
    {
      channel.Close();
      connection.Close();
    }
  }
}
