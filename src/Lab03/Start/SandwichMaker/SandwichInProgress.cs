using System;
using System.Collections.Generic;
using System.Text;

namespace SandwichMaker
{
  public class SandwichInProgress
  {
    public string ReplyTo { get; set; }
    public string CorrelationId { get; set; }
    public Messages.SandwichRequest Request { get; set; }
    public bool? GotMeat { get; set; }
    public bool? GotBread { get; set; }
    public bool? GotCheese { get; set; }
    public bool? GotLettuce { get; set; }

    public bool IsComplete
    {
      get
      {
        return
          (GotMeat.HasValue || string.IsNullOrWhiteSpace(Request.Meat)) &&
          (GotBread.HasValue || string.IsNullOrWhiteSpace(Request.Bread)) &&
          (GotCheese.HasValue || string.IsNullOrWhiteSpace(Request.Cheese)) &&
          (GotLettuce.HasValue || !Request.Lettuce);
      }
    }

    public bool Failed
    {
      get
      {
        return
          IsComplete &&
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
