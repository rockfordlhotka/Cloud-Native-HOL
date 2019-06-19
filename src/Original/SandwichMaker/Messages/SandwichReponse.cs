using System;
using System.Collections.Generic;
using System.Text;

namespace Messages
{
  public class SandwichResponse
  {
    public bool Success { get; set; }
    public string Description { get; set; }
    public string Error { get; set; }
  }
}
