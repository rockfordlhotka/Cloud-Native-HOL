using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitQueue
{
  /// <summary>
  /// Service bus implementation that uses a
  /// publish/subscribe model based on RabbitMQ
  /// queues
  /// </summary>
  public class ServiceBusQueues : IServiceBus
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
    public ServiceBusQueues(string hostName, string busName)
    {
      this.hostName = hostName;
      exchangeName = busName.Trim();
    }

    private void Initialize()
    {
      if (channel == null)
      {
        var factory = new ConnectionFactory() { HostName = hostName };
        connection = factory.CreateConnection();
        channel = connection.CreateModel();
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
      channel.QueueDeclare(
        queue: $"{exchangeName}-{bindingKey.Trim()}",
        durable: false,
        exclusive: false,
        autoDelete: false,
        arguments: null);

      var serialized = JsonConvert.SerializeObject(message);
      var body = Encoding.UTF8.GetBytes(serialized);

      var props = channel.CreateBasicProperties();
      props.CorrelationId = correlationId;

      channel.BasicPublish(
        exchange: "",
        routingKey: bindingKey,
        basicProperties: props,
        body: body);
    }

    /// <summary>
    /// Start listening for messages of a single type
    /// to arrive on the bus
    /// </summary>
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
      foreach (var item in bindingKeys)
      {
        channel.QueueDeclare(
          queue: $"{exchangeName}-{item.Trim()}",
          durable: false,
          exclusive: false,
          autoDelete: false,
          arguments: null);
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
          var message = Encoding.UTF8.GetString(ea.Body);
          handleMessage(ea, message);
        };
        channel.BasicConsume(queue: item, autoAck: true, consumer: consumer);
      }
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
