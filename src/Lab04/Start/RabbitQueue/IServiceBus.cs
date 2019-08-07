using RabbitMQ.Client.Events;
using System;

namespace RabbitQueue
{
  /// <summary>
  /// Service bus interface
  /// </summary>
  public interface IServiceBus : IDisposable
  {
    /// <summary>
    /// Publish a message to the service bus
    /// </summary>
    /// <param name="bindingKey">Binding key for the message</param>
    /// <param name="correlationId">Correlation id</param>
    /// <param name="message">Message body</param>
    void Publish(string bindingKey, string correlationId, object message);
    /// <summary>
    /// Start listening for messages of a single type
    /// to arrive on the bus
    /// </summary>
    /// <param name="bindingKeys">Binding keys to subscribe</param>
    /// <typeparam name="M">Type of message</typeparam>
    /// <param name="handleMessage">Method to invoke for each message</param>
    void Subscribe<M>(string[] bindingKeys, Action<BasicDeliverEventArgs, M> handleMessage);
    /// <summary>
    /// Start listening for messages of a single type
    /// and binding key to arrive on the bus
    /// </summary>
    /// <param name="bindingKey">Binding key to subscribe</param>
    /// <typeparam name="M">Type of message</typeparam>
    /// <param name="handleMessage">Method to invoke for each message</param>
    void Subscribe<M>(string bindingKey, Action<BasicDeliverEventArgs, M> handleMessage);
    /// <summary>
    /// Start listening for messages of a single binding
    /// key to arrive on the bus
    /// </summary>
    /// <param name="bindingKey">Binding key to subscribe</param>
    /// <typeparam name="M">Type of message</typeparam>
    /// <param name="handleMessage">Method to invoke for each message</param>
    void Subscribe(string bindingKey, Action<BasicDeliverEventArgs, string> handleMessage);
    /// <summary>
    /// Start listening for messages to arrive on the bus
    /// </summary>
    /// <param name="bindingKeys">Binding keys to subscribe</param>
    /// <param name="handleMessage">Method to invoke for each message</param>
    void Subscribe(string[] bindingKeys, Action<BasicDeliverEventArgs, string> handleMessage);
  }
}
