using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using RabbitQueue;
using Microsoft.Extensions.Configuration;
using Polly;
using System;

namespace Gateway.Services
{
  public class SandwichmakerListener : IHostedService
  {
    readonly IConfiguration _config;
    readonly IWorkInProgress _wip;
    private readonly IServiceBus _bus;
    readonly Policy _retryPolicy = Policy.
      Handle<Exception>().
      WaitAndRetry(3, r => TimeSpan.FromSeconds(Math.Pow(2, r)));

    public SandwichmakerListener(IConfiguration config, IWorkInProgress wip, IServiceBus bus)
    {
      _config = config;
      _wip = wip;
      _bus = bus;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _retryPolicy.Execute(() =>
      {
        _bus.Subscribe<Messages.SandwichResponse>("SandwichResponse", (ea, response) =>
        {
          _wip.CompleteWork(ea.BasicProperties.CorrelationId, response);
        });
      });

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      _bus.Dispose();

      return Task.CompletedTask;
    }
  }
}
