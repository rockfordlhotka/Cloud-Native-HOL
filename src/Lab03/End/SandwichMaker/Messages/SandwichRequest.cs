using System;
using System.Collections.Generic;
using System.Text;

namespace Messages
{
  /// <summary>
  /// Request from customer to make sandwich
  /// </summary>
  public class SandwichRequest
  {
    public string Meat { get; set; }
    public string Bread { get; set; }
    public string Cheese { get; set; }
    public bool Lettuce { get; set; }
  }
}
