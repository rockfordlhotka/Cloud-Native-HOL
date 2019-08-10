using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LettuceService.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class LettuceBinController : ControllerBase
  {
    private static readonly object _lock = new object();
    private volatile static int _inventory = 0;

    [HttpGet]
    public string OnGet()
    {
      return "I am running; use PUT to request Lettuce";
    }

    [HttpPut]
    public Messages.LettuceBinResponse RequestLettuce(Messages.LettuceBinRequest request)
    {
      var response = new Messages.LettuceBinResponse();
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