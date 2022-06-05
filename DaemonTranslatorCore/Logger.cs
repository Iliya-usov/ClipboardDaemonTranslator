using System;

namespace DaemonTranslatorCore;

public class Logger
{
  private readonly Action<string> myOnLogEvent;

  public Logger(Action<string> onLogEvent)
  {
    myOnLogEvent = onLogEvent;
  }
  
  public void LogMessage(string message) => myOnLogEvent(message);
}