using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitQueue;

namespace SandwichMaker
{
	 class SandwichMaker
	 {
    private static Queue _queue;
    private static readonly ConcurrentDictionary<string, SandwichInProgress> _workInProgress =
      new ConcurrentDictionary<string, SandwichInProgress>();

    static async Task Main(string[] args)
    {
      var config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

      if (_queue == null)
        _queue = new Queue(config["rabbitmq:url"], "sandwichmaker");

      Console.WriteLine("### SandwichMaker starting to listen");
      _queue.StartListening(HandleMessage);

      // wait forever - we run until the container is stopped
      await new AsyncManualResetEvent().WaitAsync();
    }

    private static void HandleMessage(BasicDeliverEventArgs ea, string message)
    {
      switch (ea.BasicProperties.Type)
      {
        case "SandwichRequest":
          RequestIngredients(ea, message);
          break;
        case "MeatBinResponse":
          HandleMeatBinResponse(ea, message);
          break;
        case "BreadBinResponse":
          HandleBreadBinResponse(ea, message);
          break;
        case "CheeseBinResponse":
          HandleCheeseBinResponse(ea, message);
          break;
        case "LettuceBinResponse":
          HandleLettuceBinResponse(ea, message);
          break;
        default:
          Console.WriteLine($"### Unknown message type '{ea.BasicProperties.Type}' from {ea.BasicProperties.ReplyTo}");
          break;
      }
    }

    private static void HandleCheeseBinResponse(BasicDeliverEventArgs ea, string message)
    {
      Console.WriteLine("### SandwichMaker got cheese");
      if (!string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId) && 
        _workInProgress.TryGetValue(ea.BasicProperties.CorrelationId, out SandwichInProgress wip))
      {
        var response = JsonConvert.DeserializeObject<Messages.CheeseBinResponse>(message);
        wip.GotCheese = response.Success;
        SeeIfSandwichIsComplete(wip);
      }
      else
      {
        // got Cheese we apparently don't need, so return it
        Console.WriteLine("### Returning unneeded Cheese");
        _queue.SendReply("cheesebin", null, new Messages.CheeseBinRequest { Returning = true });
      }
    }

    private static void HandleLettuceBinResponse(BasicDeliverEventArgs ea, string message)
    {
      Console.WriteLine("### SandwichMaker got lettuce");
      if (!string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId) && 
        _workInProgress.TryGetValue(ea.BasicProperties.CorrelationId, out SandwichInProgress wip))
      {
        var response = JsonConvert.DeserializeObject<Messages.LettuceBinResponse>(message);
        wip.GotLettuce = response.Success;
        SeeIfSandwichIsComplete(wip);
      }
      else
      {
        // got lettuce we apparently don't need, so return it
        Console.WriteLine("### Returning unneeded lettuce");
        _queue.SendReply("lettucebin", null, new Messages.LettuceBinRequest { Returning = true });
      }
    }

    private static void HandleBreadBinResponse(BasicDeliverEventArgs ea, string message)
    {
      Console.WriteLine("### SandwichMaker got bread");
      if (!string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId) && 
        _workInProgress.TryGetValue(ea.BasicProperties.CorrelationId, out SandwichInProgress wip))
      {
        var response = JsonConvert.DeserializeObject<Messages.BreadBinResponse>(message);
        wip.GotBread = response.Success;
        SeeIfSandwichIsComplete(wip);
      }
      else
      {
        // got Bread we apparently don't need, so return it
        Console.WriteLine("### Returning unneeded Bread");
        _queue.SendReply("breadbin", null, new Messages.BreadBinRequest { Returning = true });
      }
    }

    private static void HandleMeatBinResponse(BasicDeliverEventArgs ea, string message)
    {
      Console.WriteLine("### SandwichMaker got meat");
      if (!string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId) && 
        _workInProgress.TryGetValue(ea.BasicProperties.CorrelationId, out SandwichInProgress wip))
      {
        var response = JsonConvert.DeserializeObject<Messages.MeatBinResponse>(message);
        wip.GotMeat = response.Success;
        SeeIfSandwichIsComplete(wip);
      }
      else
      {
        // got Meat we apparently don't need, so return it
        Console.WriteLine("### Returning unneeded Meat");
        _queue.SendReply("meatbin", null, new Messages.MeatBinRequest { Returning = true });
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
        _queue.SendReply(wip.ReplyTo, wip.CorrelationId, new Messages.SandwichResponse
        {
          Description = wip.GetDescription(),
          Success = !wip.Failed,
          Error = wip.GetFailureReason()
        });
        if (wip.Failed)
        {
          Console.WriteLine("### SandwichMaker could NOT make sandwich");
          if (wip.GotMeat.HasValue && wip.GotMeat.Value)
            _queue.SendMessage("meatbin", wip.CorrelationId, new Messages.MeatBinRequest { Meat = wip.Request.Meat, Returning = true });
          if (wip.GotBread.HasValue && wip.GotBread.Value)
            _queue.SendMessage("breadbin", wip.CorrelationId, new Messages.BreadBinRequest { Bread = wip.Request.Bread, Returning = true });
          if (wip.GotCheese.HasValue && wip.GotCheese.Value)
            _queue.SendMessage("cheesebin", wip.CorrelationId, new Messages.CheeseBinRequest { Cheese = wip.Request.Cheese, Returning = true });
          if (wip.GotLettuce.HasValue && wip.GotLettuce.Value)
            _queue.SendMessage("lettucebin", wip.CorrelationId, new Messages.LettuceBinRequest { Returning = true });
        }
        else
        {
          Console.WriteLine("### SandwichMaker says sandwich is complete");
        }
      }
    }
    
    private static void RequestIngredients(BasicDeliverEventArgs ea, string message)
    {
      var request = JsonConvert.DeserializeObject<Messages.SandwichRequest>(message);
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
        _queue.SendMessage("meatbin", ea.BasicProperties.CorrelationId,
          new Messages.MeatBinRequest { Meat = request.Meat });
      }
      if (!string.IsNullOrEmpty(request.Bread))
      {
        Console.WriteLine($"### Sandwichmaker requesting bread");
        _queue.SendMessage("breadbin", ea.BasicProperties.CorrelationId,
          new Messages.BreadBinRequest { Bread = request.Bread });
      }
      if (!string.IsNullOrEmpty(request.Cheese))
      {
        Console.WriteLine($"### Sandwichmaker requesting cheese");
        _queue.SendMessage("cheesebin", ea.BasicProperties.CorrelationId,
          new Messages.CheeseBinRequest { Cheese = request.Cheese });
      }
      if (request.Lettuce)
      {
        Console.WriteLine($"### Sandwichmaker requesting lettuce");
        _queue.SendMessage("lettucebin", ea.BasicProperties.CorrelationId,
          new Messages.LettuceBinRequest());
      }
      SeeIfSandwichIsComplete(wip);
    }
  }
}
