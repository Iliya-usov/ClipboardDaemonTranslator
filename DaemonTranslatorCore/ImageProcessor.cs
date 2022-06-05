using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DaemonTranslatorCore
{
  public class ImageProcessor
  {
    private readonly Logger myLogger;
    private readonly ImageParser myImageParser;
    private readonly SeleniumTextTranslator mySeleniumTextTranslator;

    private string myLastText;
    private readonly SequentialScheduler myScheduler;

    public ImageProcessor(Lifetime lifetime, Logger logger)
    {
      myLogger = logger;
      var tempDirectory = Path.Combine(Path.GetTempPath(), "DaemonTranslatorCore");
      myImageParser = new ImageParser(tempDirectory, logger);
      mySeleniumTextTranslator = new SeleniumTextTranslator(lifetime, logger);

      myScheduler = new SequentialScheduler(logger);

      if (Directory.Exists(tempDirectory))
        Directory.Delete(tempDirectory, true);
      
      Directory.CreateDirectory(tempDirectory);
      lifetime.OnTermination(() => Directory.Delete(tempDirectory, true));
    }

    public async Task ProcessAsync(Lifetime lifetime, byte[] pngImage)
    {
      await Task.Factory.StartNew(() => Process(lifetime, pngImage), CancellationToken.None, TaskCreationOptions.None, myScheduler);
    }

    private void Process(Lifetime lifetime, byte[] pngImage)
    {
      try
      {
        var text = myImageParser.ParseText(pngImage, lifetime.Token);

        if (lifetime.IsNotAlive) return;
        if (string.IsNullOrEmpty(text))
        {
          myLogger.LogMessage("text is empty");
          return;
        }

        if (myLastText == text)
        {
          myLogger.LogMessage("The same text. Skip");
          return;
        }
        
        myLogger.LogMessage(text);

        if (lifetime.IsNotAlive) return;
        myLastText = text;

        mySeleniumTextTranslator.ShowTranslation(text);
      }
      catch (OperationCanceledException)
      {
      }
      catch (Exception e)
      {
        myLogger.LogMessage(e.ToString());
      }
    }
  }
}