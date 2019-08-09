using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MeatService.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class MeatBinController : ControllerBase
  {
    private static readonly object _lock = new object();
    private volatile static int _inventory = 10;

    [HttpGet]
    public string OnGet()
    {
      return "I am running; use PUT to request Meat";
    }

    [HttpPut]
    public Messages.MeatBinResponse RequestMeat(Messages.MeatBinRequest request)
    {
      var response = new Messages.MeatBinResponse();
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