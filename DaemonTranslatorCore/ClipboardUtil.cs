using System;
using System.Windows;
using System.Windows.Interop;

namespace DaemonTranslatorCore
{
  public static class ClipboardUtil
  {
    public static void AddClipboardListener(Lifetime lifetime, Window windowSource, Logger logger, Action action)
    {
      if(PresentationSource.FromVisual(windowSource) is not HwndSource source)
        throw new ArgumentException(@"Window source MUST be initialized first, such as in the Window's OnSourceInitialized handler.", nameof(windowSource));

      var windowHandle = new WindowInteropHelper(windowSource).Handle;
      source.AddHook(WndProc);
      lifetime.OnTermination(() => source.RemoveHook(WndProc));

      NativeMethods.AddClipboardFormatListener(windowHandle);
      lifetime.OnTermination(() => NativeMethods.RemoveClipboardFormatListener(windowHandle));
            
      IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
      {
        if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
        {
          try
          {
            action();
          }
          catch (Exception e)
          {
            logger.LogMessage(e.ToString());
          }
          finally
          {
            handled = true;
          }
        }

        return IntPtr.Zero;
      }
    }
  }
}