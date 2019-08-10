using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Polly;

namespace Gateway.Pages
{
  public class IndexModel : PageModel
  {
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    readonly Policy _retryPolicy = Policy.
      Handle<Exception>().
      WaitAndRetry(3, r => TimeSpan.FromSeconds(Math.Pow(2, r)));

    public IndexModel(IConfiguration config, HttpClient httpClient)
    {
      _config = config;
      _httpClient = httpClient;
    }

    [BindProperty]
    public string TheMeat { get; set; }
    [BindProperty]
    public string TheBread { get; set; }

    [BindProperty]
    public string TheCheese { get; set; }

    [BindProperty]
    public bool TheLettuce { get; set; }


    [BindProperty]
    public string ReplyText { get; set; }

    public void OnGet()
    {
    }

    public async Task OnPost()
    {
      var request = new Messages.SandwichRequest
      {
        Meat = TheMeat,
        Bread = TheBread,
        Cheese = TheCheese,
        Lettuce = TheLettuce
      };
      var server = _config["sandwichmaker:url"] + "/api/sandwichmaker";

      await _retryPolicy.Execute(async () =>
      {
        using (var httpResponse = await _httpClient.PutAsJsonAsync(server, request))
        {
          if (httpResponse.IsSuccessStatusCode)
          {
            var result = await httpResponse.Content.ReadAsAsync<Messages.SandwichResponse>();
            if (result.Success)
              ReplyText = result.Description;
            else
              ReplyText = result.Error;
          }
          else
          {
            throw new HttpRequestException(
              $"Couldn't reach sandwichmaker at {server}; Response: {httpResponse.ReasonPhrase}");
          }
        }
      });
    }
  }
}