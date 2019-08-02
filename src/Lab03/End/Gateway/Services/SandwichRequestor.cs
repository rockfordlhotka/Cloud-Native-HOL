using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using RabbitQueue;

namespace Gateway.Services
{
  public interface ISandwichRequestor
  {
      Task<Messages.SandwichResponse> RequestSandwich(Messages.SandwichRequest request);
  }

  public class SandwichRequestor : ISandwichRequestor
  {
    readonly IConfiguration _config;
    readonly IWorkInProgress _wip;

    public SandwichRequestor(IConfiguration config, IWorkInProgress wip)
    {
      _config = config;
      _wip = wip;
    }

    public async Task<Messages.SandwichResponse> RequestSandwich(Messages.SandwichRequest request)
    {
      var result = new Messages.SandwichResponse();
      var requestToCook = new Messages.SandwichRequest
      {
        Meat = request.Meat,
        Bread = request.Bread,
        Cheese = request.Cheese,
        Lettuce = request.Lettuce
      };
      var correlationId = Guid.NewGuid().ToString();
      var lockEvent = new AsyncManualResetEvent();
      _wip.StartWork(correlationId, lockEvent);
      try
      {
        using (var _queue = new Queue(_config["rabbitmq:url"], "customer"))
        {
          _queue.SendMessage("sandwichmaker", correlationId, requestToCook);
        }
        var messageArrived = lockEvent.WaitAsync();
        if (await Task.WhenAny(messageArrived, Task.Delay(10000)) == messageArrived)
        {
          result = _wip.FinalizeWork(correlationId);
        }
        else
        {
          result.Error = "The cook didn't get back to us in time, no sandwich";
          result.Success = false;
        }
      }
      finally
      {
          _wip.FinalizeWork(correlationId);
      }

      return result;
    }
  }
}