using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitQueue;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Gateway.Services
{
  public class SandwichmakerListener : IHostedService
  {
    readonly IConfiguration _config;
    readonly IWorkInProgress _wip;
    private readonly Queue _queue;

    public SandwichmakerListener(IConfiguration config, IWorkInProgress wip)
    {
      _config = config;
      _wip = wip;
      _queue = new Queue(_config["rabbitmq:url"], "customer");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _queue.StartListening((ea, message) =>
      {
        var response = JsonConvert.DeserializeObject<Messages.SandwichResponse>(message);
        var result = new Messages.SandwichResponse
        {
          Success = response.Success,
          Description = $"SUCCESS: {response.Description}",
          Error = $"FAILED: {response.Error}"
        };
        _wip.CompleteWork(ea.BasicProperties.CorrelationId, result);
      });

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      _queue.Dispose();

      return Task.CompletedTask;
    }
  }
}
