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

    public SandwichmakerListener(IConfiguration config, IWorkInProgress wip)
    {
      _config = config;
      _wip = wip;
      // TODO: open connection to queue
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      // TODO: implement code to listen for messages to gateway
      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      // TODO: shut down gracefully
      return Task.CompletedTask;
    }
  }
}
