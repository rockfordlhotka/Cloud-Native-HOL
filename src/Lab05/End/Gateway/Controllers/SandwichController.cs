using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Polly;

namespace Gateway.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class SandwichController : ControllerBase
  {
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    readonly Policy _retryPolicy = Policy.
      Handle<Exception>().
      WaitAndRetry(3, r => TimeSpan.FromSeconds(Math.Pow(2, r)));

    public SandwichController(IConfiguration config, HttpClient httpClient)
    {
      _config = config;
      _httpClient = httpClient;
    }

    [HttpGet]
    public string OnGet()
    {
      return "I am running; use PUT to make a sandwich";
    }

    [HttpPut]
    public async Task<Messages.SandwichResponse> OnPut(Messages.SandwichRequest request)
    {
      var outbound = new Messages.SandwichResponse();
      var server = _config["sandwichmaker:url"] + "/api/sandwichmaker";

      await _retryPolicy.Execute(async () =>
      {
        using (var httpResponse = await _httpClient.PutAsJsonAsync(server, request))
        {
          if (httpResponse.IsSuccessStatusCode)
            outbound = await httpResponse.Content.ReadAsAsync<Messages.SandwichResponse>();
          else
            outbound.Error = $"Couldn't reach sandwichmaker at {server}; Response: {httpResponse.ReasonPhrase}";
        }
      });
      return outbound;
    }
  }
}