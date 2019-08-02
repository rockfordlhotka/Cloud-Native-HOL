using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

      // TODO: Implement code to send request to make a sandwich

      return result;
    }
  }
}
