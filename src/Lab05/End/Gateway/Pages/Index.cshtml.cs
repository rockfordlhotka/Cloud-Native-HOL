using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace Gateway.Pages
{
  public class IndexModel : PageModel
  {
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

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
          ReplyText = $"Couldn't reach sandwichmaker at {server}; Response: {httpResponse.ReasonPhrase}";
        }
      }
    }
  }
}
