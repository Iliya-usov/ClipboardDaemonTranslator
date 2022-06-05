using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DaemonTranslatorCore
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow
  {
    private readonly Lifetime myLifetime;
    private readonly ImageProcessor myImageProcessor;
    private readonly Logger myLogger;

    public MainWindow()
    {
      InitializeComponent();
      myLogger = new Logger(OnLogEvent);
      Lifetime.Logger = myLogger;
      myLifetime = new Lifetime();
      myImageProcessor = new ImageProcessor(myLifetime, myLogger);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
      base.OnSourceInitialized(e);

      var disposableState = new Lifetime();

      ClipboardUtil.AddClipboardListener(myLifetime, this,  myLogger, () =>
      {
        var newDisposable = new Lifetime();
        var oldDisposable = Interlocked.Exchange(ref disposableState, newDisposable);
        oldDisposable.Dispose();

        ProcessAsync();
        async void ProcessAsync()
        {
          try
          {
            while (true)
            {
              await Task.Delay(100, newDisposable.Token);
              if (TryGetImage(out var image))
              {
                if (image == null)
                {
                  myLogger.LogMessage("Image is null");
                  return;
                }
                
                var png = image.ToPng();
                await myImageProcessor.ProcessAsync(newDisposable, png);
                return;
              }
            }
          }
          catch (OperationCanceledException)
          {
          }
          catch (Exception exception)
          {
            myLogger.LogMessage(exception.ToString());
          }
        }
      });

      myLifetime.OnTermination(() => disposableState?.Dispose());
    }

    private static bool TryGetImage(out BitmapSource? image)
    {
      try
      {
        image = Clipboard.GetImage();
        return true;
      }
      catch
      {
        image = null;
        return false;
      }
    }


    protected override void OnClosed(EventArgs e)
    {
      Lifetime.Logger = null;
      myLifetime.Dispose();
    }

    private void OnLogEvent(string message)
    {
      Dispatcher?.Invoke(() =>
      {
        LogOutput.AppendText(message);
        LogOutput.AppendText(Environment.NewLine);
        LogOutput.ScrollToEnd();
      });
    }
  }
}