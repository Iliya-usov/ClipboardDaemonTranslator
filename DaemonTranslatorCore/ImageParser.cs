using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace DaemonTranslatorCore
{
  public class ImageParser
  {
    private readonly Logger myLogger;
    private readonly string myTempImgFile;
    private readonly string myTempOutFile;
    private readonly string myTempOutFileWithExt;

    public ImageParser(string tempDirectory, Logger logger)
    {
      myLogger = logger;
      myTempImgFile = Path.Combine(tempDirectory, "img.png");
      myTempOutFile = Path.Combine(tempDirectory, "out");
      myTempOutFileWithExt = myTempOutFile + ".txt";
    }

    public string ParseText(byte[] png, CancellationToken token)
    {
      using var disposable = new Lifetime();

      File.WriteAllBytes(myTempImgFile, png);

      disposable.OnTermination(() => File.Delete(myTempImgFile));
      disposable.OnTermination(() => File.Delete(myTempOutFileWithExt));

      var psi = new ProcessStartInfo(@"C:\Program Files\Tesseract-OCR\tesseract.exe")
      {
        Arguments = $"{myTempImgFile} {myTempOutFile}",
        CreateNoWindow = true,
        UseShellExecute = false
        // todo output?
      };
      
      var process = Process.Start(psi);
      if (process == null) throw new NullReferenceException($"Process is null: {psi}");

      var task = process.WaitForExitAsync(token);
      SpinWait.SpinUntil(() => task.IsCompleted);

      if (task.IsCompletedSuccessfully)
      {
        myLogger.LogMessage("Test parsed successfully");
        return File.ReadAllText(myTempOutFileWithExt);
      }
      
      KillSafe(process);
      process.WaitForExit();

      token.ThrowIfCancellationRequested();
      task.Wait(token);
      throw new InvalidOperationException("Must not be reached");
    }

    private static void KillSafe(Process process)
    {
      try
      {
        process.Kill();
      }
      catch (Exception e)
      {
      }
    }
  }
}