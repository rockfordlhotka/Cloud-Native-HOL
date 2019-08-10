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
    }
  }
}
