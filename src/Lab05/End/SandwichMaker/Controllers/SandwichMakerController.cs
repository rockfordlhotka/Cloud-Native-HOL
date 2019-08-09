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
        result.Description = wip.GetDescription();
      else
        result.Error = wip.GetFailureReason();

      return result;
    }

    private void HandleMessage(object response, SandwichInProgress sandwich)
    {
      lock (sandwich)
      {
        switch (response.GetType().Name)
        {
          case "MeatBinResponse":
            sandwich.GotMeat = ((Messages.MeatBinResponse)response).Success;
            break;
          case "BreadBinResponse":
            sandwich.GotBread = ((Messages.BreadBinResponse)response).Success;
            break;
          case "CheeseBinResponse":
            sandwich.GotCheese = ((Messages.CheeseBinResponse)response).Success;
            break;
          case "LettuceBinResponse":
            sandwich.GotLettuce = ((Messages.LettuceBinResponse)response).Success;
            break;
          default:
            Console.WriteLine($"### Unknown message type '{response.GetType().Name}'");
            break;
        }
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