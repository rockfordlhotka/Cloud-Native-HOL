using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Gateway.Pages
{
  public class IndexModel : PageModel
  {
    readonly Services.ISandwichRequestor _requestor;

    public IndexModel(Services.ISandwichRequestor requestor)
    {
      _requestor = requestor;
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
      var result = await _requestor.RequestSandwich(request);

      if (result.Success)
        ReplyText = result.Description;
      else
        ReplyText = result.Error;
    }
  }
}
