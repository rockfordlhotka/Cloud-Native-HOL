// Code from Stephen Toub @ Microsoft
// https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-1-asyncmanualresetevent/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitQueue
{
  public class AsyncManualResetEvent
  {
    private volatile TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

    public Task WaitAsync() { return _tcs.Task; }

    public void Set() { _tcs.TrySetResult(true); }
    public void Reset()
    {
      while (true)
      {
        var tcs = _tcs;
        if (!tcs.Task.IsCompleted ||
            Interlocked.CompareExchange(ref _tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
          return;
      }
    }
  }
}
