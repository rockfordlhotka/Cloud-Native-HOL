using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Polly;

namespace SandwichMaker.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class SandwichMakerController : ControllerBase
  {
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    readonly Policy _retryPolicy = Policy.
      Handle<Exception>().
      WaitAndRetry(3, r => TimeSpan.FromSeconds(Math.Pow(2, r)));

    public SandwichMakerController(IConfiguration config, HttpClient httpClient)
    {
      _config = config;
      _httpClient = httpClient;
    }

#if DEBUG
    [HttpGet]
    public string OnGet()
    {
      return "I am running; use PUT to request a sandwich";
    }
#endif

    [HttpPut]
    public async Task<Messages.SandwichResponse> OnPut(Messages.SandwichRequest request)
    {
      Console.WriteLine($"### Sandwichmaker making {request.Meat} on {request.Bread}{Environment.NewLine} at {DateTime.Now}");
      var wip = new SandwichInProgress(request);

      var requests = new List<Task>
      {
        RequestBread(wip, new Messages.BreadBinRequest { Bread = wip.Request.Bread }),
        RequestMeat(wip, new Messages.MeatBinRequest { Meat = wip.Request.Meat }),
        RequestCheese(wip, new Messages.CheeseBinRequest { Cheese = wip.Request.Cheese }),
        RequestLettuce(wip, new Messages.LettuceBinRequest { Returning = false })
      };

      var result = new Messages.SandwichResponse();

      var timeout = Task.Delay(10000);
      if (await Task.WhenAny(Task.WhenAll(requests), timeout) == timeout)
      {
        await ReturnInventory(wip);
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

    private Task RequestBread(SandwichInProgress wip, Messages.BreadBinRequest request)
    {
      if (string.IsNullOrEmpty(wip.Request.Bread)) return Task.CompletedTask;

      return _retryPolicy.ExecuteAndCapture<Task>(() =>
        _httpClient.PutAsJsonAsync(
          _config["breadservice:url"] + "/api/breadbin",
          request).
          ContinueWith(async (e) => HandleMessage(await e.Result.Content.ReadAsAsync<Messages.BreadBinResponse>(), wip))
      ).Result;
    }

    private Task RequestMeat(SandwichInProgress wip, Messages.MeatBinRequest request)
    {
      if (string.IsNullOrEmpty(wip.Request.Meat)) return Task.CompletedTask;

      return _retryPolicy.ExecuteAndCapture<Task>(() =>
        _httpClient.PutAsJsonAsync(
          _config["Meatservice:url"] + "/api/Meatbin",
          request).
          ContinueWith(async (e) => HandleMessage(await e.Result.Content.ReadAsAsync<Messages.MeatBinResponse>(), wip))
      ).Result;
    }

    private Task RequestCheese(SandwichInProgress wip, Messages.CheeseBinRequest request)
    {
      if (string.IsNullOrEmpty(wip.Request.Cheese)) return Task.CompletedTask;

      return _retryPolicy.ExecuteAndCapture<Task>(() =>
        _httpClient.PutAsJsonAsync(
          _config["Cheeseservice:url"] + "/api/Cheesebin",
          request).
          ContinueWith(async (e) => HandleMessage(await e.Result.Content.ReadAsAsync<Messages.CheeseBinResponse>(), wip))
      ).Result;
    }

    private Task RequestLettuce(SandwichInProgress wip, Messages.LettuceBinRequest request)
    {
      if (!wip.Request.Lettuce) return Task.CompletedTask;

      return _retryPolicy.ExecuteAndCapture<Task>(() =>
        _httpClient.PutAsJsonAsync(
          _config["Lettuceservice:url"] + "/api/Lettucebin",
          request).
          ContinueWith(async (e) => HandleMessage(await e.Result.Content.ReadAsAsync<Messages.LettuceBinResponse>(), wip))
      ).Result;
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
      var requests = new List<Task>
      {
        RequestBread(wip, new Messages.BreadBinRequest { Bread = wip.Request.Bread, Returning = true }),
        RequestMeat(wip, new Messages.MeatBinRequest { Meat = wip.Request.Meat, Returning = true }),
        RequestCheese(wip, new Messages.CheeseBinRequest { Cheese = wip.Request.Cheese, Returning = true }),
        RequestLettuce(wip, new Messages.LettuceBinRequest { Returning = true })
      };

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