using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DaemonTranslatorCore;

public class SequentialScheduler : TaskScheduler
{
  private readonly Channel<Task> myChannel;
  private readonly SyncContext mySyncContext;

  public SequentialScheduler(Logger logger, TaskScheduler realScheduler = null)
  {
    var options = new UnboundedChannelOptions { SingleReader = true, AllowSynchronousContinuations = false };
    myChannel = Channel.CreateUnbounded<Task>(options);
    mySyncContext = new SyncContext(this);

    Task.Factory.StartNew(async () =>
    {
      while (true)
      {
        try
        {
          var task = await myChannel.Reader.ReadAsync();
          ExecuteTaskWithContext(task);
        }
        catch (Exception e)
        {
          logger.LogMessage(e.ToString());
        }
      }
    }, CancellationToken.None, TaskCreationOptions.None, realScheduler ?? Default);
  }
    
  protected override void QueueTask(Task task)
  {
    if (!myChannel.Writer.TryWrite(task))
      throw new ObjectDisposedException(nameof(SequentialScheduler));
  }

  private bool ExecuteTaskWithContext(Task task)
  {
    var old = SynchronizationContext.Current;
    try
    {
      SynchronizationContext.SetSynchronizationContext(mySyncContext);
      return TryExecuteTask(task);
    }
    finally
    {
      SynchronizationContext.SetSynchronizationContext(old);
    }
  }

  protected override IEnumerable<Task> GetScheduledTasks() => null;
  protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;
    
  private class SyncContext : SynchronizationContext
  {
    private readonly SequentialScheduler myScheduler;

    public SyncContext(SequentialScheduler scheduler) => myScheduler = scheduler;
    public override void Post(SendOrPostCallback d, object state) => Task.Factory.StartNew(() => d(state), CancellationToken.None, TaskCreationOptions.None, myScheduler);
  }
}