using System;
using Microsoft.AspNetCore.Mvc;

namespace CheeseService.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class CheeseBinController : ControllerBase
  {
    private static readonly object _lock = new object();
    private volatile static int _inventory = 10;

    [HttpGet]
    public string OnGet()
    {
      return "I am running; use PUT to request Cheese";
    }

    [HttpPut]
    public Messages.CheeseBinResponse RequestCheese(Messages.CheeseBinRequest request)
    {
      var response = new Messages.CheeseBinResponse();
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