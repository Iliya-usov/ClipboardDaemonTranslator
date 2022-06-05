using System;
using System.IO;
using OpenQA.Selenium.Chrome;

namespace DaemonTranslatorCore
{
  public class SeleniumTextTranslator
  {
    private readonly Logger myLogger;

    private readonly ChromeDriver myDriver;

    public SeleniumTextTranslator(Lifetime lifetime, Logger logger)
    {
      myLogger = logger;
      var currentDirectory = Directory.GetCurrentDirectory();
      var driverService = ChromeDriverService.CreateDefaultService(currentDirectory);        
      driverService.Start();
      myDriver = new ChromeDriver(driverService);
      lifetime.OnTermination(() => myDriver.Dispose());
    }

    public void ShowTranslation(string text)
    {
      var outputText = text
        .Replace("\r", "")
        .Replace("\n", " ")
        .Replace("|", "I")
        .Replace("/", "l");

      var escapedTest = Uri.EscapeDataString(outputText);
      var url = "https://www.deepl.com/ru/translator#en/ru/" + escapedTest;

      myLogger.LogMessage($"Translate text: {url}");
      myDriver.Url = url;
    }
  }
}