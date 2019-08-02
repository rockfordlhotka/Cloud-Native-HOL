using Messages;
using RabbitQueue;
using System.Collections.Generic;

namespace Gateway.Services
{
  public interface IWorkInProgress
  {
    void StartWork(string correlationId, AsyncManualResetEvent lockEvent);
    void CompleteWork(string correlationId, Messages.SandwichResponse response);
    SandwichResponse FinalizeWork(string correlationId);
  }

  public class WorkInProgress : IWorkInProgress
  {
    readonly Dictionary<string, WipItem> WipList =
      new Dictionary<string, WipItem>();

    /// <summary>
    /// Start a work item (call when sending request to system)
    /// </summary>
    public void StartWork(string correlationId, AsyncManualResetEvent lockEvent)
    {
      WipList.Add(correlationId, new WipItem { Lock = lockEvent });
    }

    /// <summary>
    /// Complete a work item (call when receiving reply from system)
    /// </summary>
    public void CompleteWork(string correlationId, SandwichResponse response)
    {
      if (WipList.TryGetValue(correlationId, out WipItem wipItem))
      {
        wipItem.Response = response;
        wipItem.Lock.Set();
      }
      else
      {
        throw new KeyNotFoundException(correlationId);
      }
    }

    /// <summary>
    /// Finalize a work item (call to get/process reply,
    /// and to remove work item)
    /// </summary>
    public SandwichResponse FinalizeWork(string correlationId)
    {
      SandwichResponse result = null;
      if (WipList.TryGetValue(correlationId, out WipItem wipItem))
      {
        result = wipItem.Response;
        WipList.Remove(correlationId);
      }
      return result;
    }

    private class WipItem
    {
      public AsyncManualResetEvent Lock { get; set; }
      public Messages.SandwichResponse Response { get; set; }
    }
  }

}
