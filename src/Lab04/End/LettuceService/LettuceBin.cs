using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitQueue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LettuceService
{
  class LettuceBin
  {
    private static ServiceBus _bus;

    static async Task Main(string[] args)
    {
      var config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

      if (_bus == null)
        _bus = new ServiceBus(config["rabbitmq:url"], "sandwichBus");

      Console.WriteLine("### Lettuce bin service starting to listen");
      _bus.Subscribe<Messages.LettuceBinRequest>("LettuceBinRequest", HandleMessage);

      // wait forever - we run until the container is stopped
      await new AsyncManualResetEvent().WaitAsync();
    }

    private volatile static int _inventory = 0;

    private static void HandleMessage(BasicDeliverEventArgs ea, Messages.LettuceBinRequest request)
    {
      var response = new Messages.LettuceBinResponse();
      lock (_bus)
      {
        if (request.Returning)
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - returned");
          _inventory++;
        }
        else if (_inventory > 0)
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - filled");
          _inventory--;
          response.Success = true;
          _bus.Publish("LettuceBinResponse", ea.BasicProperties.CorrelationId, response);
        }
        else
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - no inventory");
          response.Success = false;
          _bus.Publish("LettuceBinResponse", ea.BasicProperties.CorrelationId, response);
        }
      }
    }
  }
}
