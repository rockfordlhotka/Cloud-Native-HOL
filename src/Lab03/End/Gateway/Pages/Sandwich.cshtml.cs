using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitQueue;

namespace Gateway.Pages
{
  public class SandwichModel : PageModel
  {
    readonly IConfiguration _config;

    public SandwichModel(IConfiguration config)
    {
      _config = config;
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
      using (var _queue = new Queue(_config["rabbitmq:url"], "customer"))
      {
        var reset = new AsyncManualResetEvent();
        _queue.StartListening((ea, message) =>
        {
          var response = JsonConvert.DeserializeObject<Messages.SandwichResponse>(message);
          if (response.Success)
            ReplyText = $"SUCCESS: {response.Description}";
          else
            ReplyText = $"FAILED: {response.Error}";
          reset.Set();
        });

        var request = new Messages.SandwichRequest
        {
          Meat = TheMeat,
          Bread = TheBread,
          Cheese = TheCheese,
          Lettuce = TheLettuce
        };
        _queue.SendMessage("sandwichmaker", Guid.NewGuid().ToString(), request);

        var task = reset.WaitAsync();
        if (await Task.WhenAny(task, Task.Delay(10000)) != task)
          ReplyText = "The cook didn't get back to us in time, no sandwich";
      }
    }
  }
}