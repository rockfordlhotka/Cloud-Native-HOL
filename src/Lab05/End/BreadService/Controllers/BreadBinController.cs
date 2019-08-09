using System;
using Microsoft.AspNetCore.Mvc;

namespace BreadService.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class BreadBinController : ControllerBase
  {
    private static readonly object _lock = new object();
    private volatile static int _inventory = 10;

    [HttpGet]
    public string OnGet()
    {
      return "I am running; use PUT to request Bread";
    }

    [HttpPut]
    public Messages.BreadBinResponse RequestBread(Messages.BreadBinRequest request)
    {
      var response = new Messages.BreadBinResponse();
      lock (_lock)
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
        }
        else
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - no inventory");
          response.Success = false;
        }
      }
      return response;
    }
  }
}