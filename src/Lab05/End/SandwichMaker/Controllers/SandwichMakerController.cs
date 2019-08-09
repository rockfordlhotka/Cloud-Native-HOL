using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace SandwichMaker.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class SandwichMakerController : ControllerBase
  {
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public SandwichMakerController(IConfiguration config, HttpClient httpClient)
    {
      _config = config;
      _httpClient = httpClient;
    }

    [HttpGet]
    public string OnGet()
    {
      return "I am running; use PUT to request a sandwich";
    }

    [HttpPut]
    public async Task<Messages.SandwichResponse> RequestSandwich(Messages.SandwichRequest request)
    {
      Console.WriteLine($"### Sandwichmaker making {request.Meat} on {request.Bread}{Environment.NewLine} at {DateTime.Now}");

      var wip = new SandwichInProgress(request);

      var requests = new List<Task>();
      if (!string.IsNullOrEmpty(request.Bread))
        requests.Add(_httpClient.PutAsJsonAsync(
          _config["breadservice:url"] + "/api/breadbin",
          new Messages.BreadBinRequest { Bread = request.Bread }).
          ContinueWith(async (e)=> HandleMessage(await e.Result.Content.ReadAsAsync<Messages.BreadBinResponse>(), wip)));
      if (!string.IsNullOrEmpty(request.Meat))
        requests.Add(_httpClient.PutAsJsonAsync(
          _config["meatservice:url"] + "/api/meatbin",
          new Messages.MeatBinRequest { Meat = request.Meat }).
          ContinueWith(async (e) => HandleMessage(await e.Result.Content.ReadAsAsync<Messages.MeatBinResponse>(), wip)));
      if (!string.IsNullOrEmpty(request.Cheese))
        requests.Add(_httpClient.PutAsJsonAsync(
          _config["cheeseservice:url"] + "/api/cheesebin",
          new Messages.CheeseBinRequest { Cheese = request.Cheese }).
          ContinueWith(async (e) => HandleMessage(await e.Result.Content.ReadAsAsync<Messages.CheeseBinResponse>(), wip)));
      if (request.Lettuce)
        requests.Add(_httpClient.PutAsJsonAsync(
          _config["lettuceservice:url"] + "/api/lettucebin",
          new Messages.LettuceBinRequest { Returning = false }).
          ContinueWith(async (e) => HandleMessage(await e.Result.Content.ReadAsAsync<Messages.LettuceBinResponse>(), wip)));

      var result = new Messages.SandwichResponse();

      var timeout = Task.Delay(10000);
      if (await Task.WhenAny(Task.WhenAll(requests), timeout) == timeout)
      {
        result.Error = "The cook didn't get back to us in time, no sandwich";
        result.Success = false;
        return result;
      }

      result.Success = !wip.Failed;
      if (result.Success)
      {
        result.Description = wip.GetDescription();
      }
      else
      {
        result.Error = wip.GetFailureReason();
        await ReturnInventory(wip);
      }

      return result;
    }

    private void HandleMessage(Messages.MeatBinResponse response, SandwichInProgress wip)
    {
      lock (wip)
      {
        wip.GotMeat = response.Success;
      }
    }

    private void HandleMessage(Messages.BreadBinResponse response, SandwichInProgress wip)
    {
      lock (wip)
      {
        wip.GotBread = response.Success;
      }
    }

    private void HandleMessage(Messages.CheeseBinResponse response, SandwichInProgress wip)
    {
      lock (wip)
      {
        wip.GotCheese = response.Success;
      }
    }

    private void HandleMessage(Messages.LettuceBinResponse response, SandwichInProgress wip)
    {
      lock (wip)
      {
        wip.GotLettuce = response.Success;
      }
    }

  private async Task ReturnInventory(SandwichInProgress wip)
    {
      var requests = new List<Task>();
      if (wip.GotBread.HasValue && wip.GotBread.Value)
        requests.Add(_httpClient.PutAsJsonAsync(
          _config["breadservice:url"] + "/api/breadbin",
          new Messages.BreadBinRequest { Bread = wip.Request.Bread, Returning = true }));
      if (wip.GotMeat.HasValue && wip.GotMeat.Value)
        requests.Add(_httpClient.PutAsJsonAsync(
          _config["meatservice:url"] + "/api/meatbin",
          new Messages.MeatBinRequest { Meat = wip.Request.Meat, Returning = true }));
      if (wip.GotCheese.HasValue && wip.GotCheese.Value)
        requests.Add(_httpClient.PutAsJsonAsync(
          _config["cheeseservice:url"] + "/api/cheesebin",
          new Messages.CheeseBinRequest { Cheese = wip.Request.Cheese, Returning = true }));
      if (wip.GotLettuce.HasValue && wip.GotLettuce.Value)
        requests.Add(_httpClient.PutAsJsonAsync(
          _config["lettuceservice:url"] + "/api/lettucebin",
          new Messages.LettuceBinRequest { Returning = true }));

      var timeout = Task.Delay(10000);
      if (await Task.WhenAny(Task.WhenAll(requests), timeout) == timeout)
      {
        Console.WriteLine($"### Timeout returning inventory");
        throw new TimeoutException("ReturnInventory");
      }
    }

    private class SandwichInProgress
    {
      public Messages.SandwichRequest Request { get; set; }
      public bool? GotMeat { get; set; }
      public bool? GotBread { get; set; }
      public bool? GotCheese { get; set; }
      public bool? GotLettuce { get; set; }

      public SandwichInProgress(Messages.SandwichRequest request)
      {
        Request = request;
      }

      public bool Failed
      {
        get
        {
          return
            (GotMeat.HasValue && !GotMeat.Value) ||
            (GotBread.HasValue && !GotBread.Value) ||
            (GotCheese.HasValue && !GotCheese.Value) ||
            (GotLettuce.HasValue && !GotLettuce.Value);
        }
      }

      public string GetDescription()
      {
        var result = $"{Request.Meat} on {Request.Bread} with {Request.Cheese}";
        if (Request.Lettuce)
          result += " and lettuce";
        return result;
      }

      public string GetFailureReason()
      {
        if (Failed)
        {
          if (GotMeat.HasValue && !GotMeat.Value)
            return "No meat";
          if (GotBread.HasValue && !GotBread.Value)
            return "No bread";
          if (GotCheese.HasValue && !GotCheese.Value)
            return "No cheese";
          if (GotLettuce.HasValue && !GotLettuce.Value)
            return "No lettuce";
          return $"The cook failed to make {GetDescription()}";
        }
        return string.Empty;
      }
    }
  }
}