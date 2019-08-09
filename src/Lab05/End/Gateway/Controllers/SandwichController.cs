using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class SandwichController : ControllerBase
  {
    [HttpGet]
    public string OnGet()
    {
      return "I am running; use PUT to make a sandwich";
    }

    [HttpPut]
    public async Task<Messages.SandwichResponse> OnPut(Messages.SandwichRequest request)
    {
      //return await _requestor.RequestSandwich(request);
      return null;
    }
  }
}