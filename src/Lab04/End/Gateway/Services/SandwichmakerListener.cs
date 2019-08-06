using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using RabbitQueue;
using Microsoft.Extensions.Configuration;

namespace Gateway.Services
{
  public class SandwichmakerListener : IHostedService
  {
    readonly IConfiguration _config;
    readonly IWorkInProgress _wip;
    private readonly ServiceBus _bus;

    public SandwichmakerListener(IConfiguration config, IWorkInProgress wip)
    {
      _config = config;
      _wip = wip;
      _bus = new ServiceBus(_config["rabbitmq:url"], "sandwichBus");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _bus.Subscribe<Messages.SandwichResponse>("SandwichResponse", (ea, response) =>
      {
        _wip.CompleteWork(ea.BasicProperties.CorrelationId, response);
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
