using System;
using System.Collections.Immutable;
using System.Threading;

namespace DaemonTranslatorCore
{
  public class Lifetime : IDisposable
  {
    public static Logger? Logger { get; set; }
    
    private readonly CancellationTokenSource mySource = new(); 
    private ImmutableArray<object>? myObjects = ImmutableArray<object>.Empty;

    public bool IsAlive => myObjects.HasValue;
    public bool IsNotAlive => !IsAlive;

    public CancellationToken Token => mySource.Token;

    public void OnTermination(Action action)
    {
      if (!TryOnTerminationInternal(action))
      {
        action();
        throw new ObjectDisposedException(nameof(Lifetime));
      }
    }

    public void OnTermination(IDisposable action)
    {
      if (!TryOnTerminationInternal(action))
      {
        action.Dispose();
        throw new ObjectDisposedException(nameof(Lifetime));
      }
    }

    private bool TryOnTerminationInternal(object o)
    {
      lock (mySource)
      {
        if (myObjects is { } objects)
        {
          myObjects = objects.Add(o);
          return true;
        }
      }

      return false;
    }

    public void Dispose()
    {
      mySource.Cancel();
      
      ImmutableArray<object> array;
      lock (this)
      {
        if (myObjects is { } objects)
        {
          array = objects;
          myObjects = null;
        }
        else return;
      }

      for (var i = array.Length - 1; i >= 0; i--)
      {
        try
        {
          switch (array[i])
          {
            case Action action:
              action();
              break;
            
            case IDisposable disposable:
              disposable.Dispose();
              break;
          }
        }
        catch (Exception e)
        {
          if (Logger is {} logger)
            logger.LogMessage(e.ToString());
          else
            Console.Error.WriteLine(e);
        }
      }
      
      GC.SuppressFinalize(this);
    }
  }
}