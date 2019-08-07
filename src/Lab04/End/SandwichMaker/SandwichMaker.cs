using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitQueue;

namespace SandwichMaker
{
	 class SandwichMaker
	 {
    private static IServiceBus _bus;
    private static readonly ConcurrentDictionary<string, SandwichInProgress> _workInProgress =
      new ConcurrentDictionary<string, SandwichInProgress>();

    static async Task Main(string[] args)
    {
      var config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

      if (_bus == null)
        _bus = new ServiceBus(config["rabbitmq:url"], "sandwichBus");

      Console.WriteLine("### SandwichMaker starting to listen");

      //_bus.Subscribe<Messages.SandwichRequest>(
      //  new string[] { "SandwichRequest", "MeatBinResponse", "BreadBinResponse", "CheeseBinResponse", "LettuceBinResponse" },
      //  HandleMessage);

      _bus.Subscribe<Messages.SandwichRequest>(
        "SandwichRequest",
        RequestIngredients);
      _bus.Subscribe<Messages.MeatBinResponse>(
        "MeatBinResponse",
        HandleMeatBinResponse);
      _bus.Subscribe<Messages.BreadBinResponse>(
        "BreadBinResponse",
        HandleBreadBinResponse);
      _bus.Subscribe<Messages.CheeseBinResponse>(
        "CheeseBinResponse",
        HandleCheeseBinResponse);
      _bus.Subscribe<Messages.LettuceBinResponse>(
        "LettuceBinResponse",
        HandleLettuceBinResponse);

      // wait forever - we run until the container is stopped
      await new AsyncManualResetEvent().WaitAsync();
    }

    private static void HandleCheeseBinResponse(BasicDeliverEventArgs ea, Messages.CheeseBinResponse response)
    {
      Console.WriteLine("### SandwichMaker got cheese");
      if (!string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId) && 
        _workInProgress.TryGetValue(ea.BasicProperties.CorrelationId, out SandwichInProgress wip))
      {
        wip.GotCheese = response.Success;
        SeeIfSandwichIsComplete(wip);
      }
      else
      {
        // got Cheese we apparently don't need, so return it
        Console.WriteLine("### Returning unneeded Cheese");
        _bus.Publish("CheeseBinRequest", null, new Messages.CheeseBinRequest { Returning = true });
      }
    }

    private static void HandleLettuceBinResponse(BasicDeliverEventArgs ea, Messages.LettuceBinResponse response)
    {
      Console.WriteLine("### SandwichMaker got lettuce");
      if (!string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId) && 
        _workInProgress.TryGetValue(ea.BasicProperties.CorrelationId, out SandwichInProgress wip))
      {
        wip.GotLettuce = response.Success;
        SeeIfSandwichIsComplete(wip);
      }
      else
      {
        // got lettuce we apparently don't need, so return it
        Console.WriteLine("### Returning unneeded lettuce");
        _bus.Publish("LettuceBinRequest", null, new Messages.LettuceBinRequest { Returning = true });
      }
    }

    private static void HandleBreadBinResponse(BasicDeliverEventArgs ea, Messages.BreadBinResponse response)
    {
      Console.WriteLine("### SandwichMaker got bread");
      if (!string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId) && 
        _workInProgress.TryGetValue(ea.BasicProperties.CorrelationId, out SandwichInProgress wip))
      {
        wip.GotBread = response.Success;
        SeeIfSandwichIsComplete(wip);
      }
      else
      {
        // got Bread we apparently don't need, so return it
        Console.WriteLine("### Returning unneeded Bread");
        _bus.Publish("BreadBinRequest", null, new Messages.BreadBinRequest { Returning = true });
      }
    }

    private static void HandleMeatBinResponse(BasicDeliverEventArgs ea, Messages.MeatBinResponse response)
    {
      Console.WriteLine("### SandwichMaker got meat");
      if (!string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId) && 
        _workInProgress.TryGetValue(ea.BasicProperties.CorrelationId, out SandwichInProgress wip))
      {
        wip.GotMeat = response.Success;
        SeeIfSandwichIsComplete(wip);
      }
      else
      {
        // got Meat we apparently don't need, so return it
        Console.WriteLine("### Returning unneeded Meat");
        _bus.Publish("MeatBinRequest", null, new Messages.MeatBinRequest { Returning = true });
      }
    }

    private static void SeeIfSandwichIsComplete(SandwichInProgress wip)
    {
      if (wip.IsComplete)
      {
        Console.WriteLine($"### SandwichMaker is done with {wip.CorrelationId}");
        if (!_workInProgress.TryRemove(wip.CorrelationId, out SandwichInProgress temp))
        {
          var id = wip.CorrelationId;
          if (temp != null) id = temp.CorrelationId;
          Console.WriteLine($"### SandwichMaker could NOT remove WIP {id}");
        }
        _bus.Publish("SandwichResponse", wip.CorrelationId, new Messages.SandwichResponse
        {
          Description = wip.GetDescription(),
          Success = !wip.Failed,
          Error = wip.GetFailureReason()
        });
        if (wip.Failed)
        {
          Console.WriteLine("### SandwichMaker could NOT make sandwich");
          if (wip.GotMeat.HasValue && wip.GotMeat.Value)
            _bus.Publish("MeatBinRequest", wip.CorrelationId, new Messages.MeatBinRequest { Meat = wip.Request.Meat, Returning = true });
          if (wip.GotBread.HasValue && wip.GotBread.Value)
            _bus.Publish("BreadBinRequest", wip.CorrelationId, new Messages.BreadBinRequest { Bread = wip.Request.Bread, Returning = true });
          if (wip.GotCheese.HasValue && wip.GotCheese.Value)
            _bus.Publish("CheeseBinRequest", wip.CorrelationId, new Messages.CheeseBinRequest { Cheese = wip.Request.Cheese, Returning = true });
          if (wip.GotLettuce.HasValue && wip.GotLettuce.Value)
            _bus.Publish("LettuceBinRequest", wip.CorrelationId, new Messages.LettuceBinRequest { Returning = true });
        }
        else
        {
          Console.WriteLine("### SandwichMaker says sandwich is complete");
        }
      }
    }
    
    private static void RequestIngredients(BasicDeliverEventArgs ea, Messages.SandwichRequest request)
    {
      var wip = new SandwichInProgress
      {
        ReplyTo = ea.BasicProperties.ReplyTo,
        CorrelationId = ea.BasicProperties.CorrelationId,
        Request = request
      };
      _workInProgress.TryAdd(ea.BasicProperties.CorrelationId, wip);
      Console.WriteLine($"### Sandwichmaker making {request.Meat} on {request.Bread}{Environment.NewLine}  from {ea.BasicProperties.CorrelationId} at {DateTime.Now}");

      if (!string.IsNullOrEmpty(request.Meat))
      {
        Console.WriteLine($"### Sandwichmaker requesting meat");
        _bus.Publish("MeatBinRequest", ea.BasicProperties.CorrelationId,
          new Messages.MeatBinRequest { Meat = request.Meat });
      }
      if (!string.IsNullOrEmpty(request.Bread))
      {
        Console.WriteLine($"### Sandwichmaker requesting bread");
        _bus.Publish("BreadBinRequest", ea.BasicProperties.CorrelationId,
          new Messages.BreadBinRequest { Bread = request.Bread });
      }
      if (!string.IsNullOrEmpty(request.Cheese))
      {
        Console.WriteLine($"### Sandwichmaker requesting cheese");
        _bus.Publish("CheeseBinRequest", ea.BasicProperties.CorrelationId,
          new Messages.CheeseBinRequest { Cheese = request.Cheese });
      }
      if (request.Lettuce)
      {
        Console.WriteLine($"### Sandwichmaker requesting lettuce");
        _bus.Publish("LettuceBinRequest", ea.BasicProperties.CorrelationId,
          new Messages.LettuceBinRequest());
      }
      SeeIfSandwichIsComplete(wip);
    }
  }
}
