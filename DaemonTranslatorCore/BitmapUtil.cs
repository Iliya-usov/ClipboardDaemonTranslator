using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace DaemonTranslatorCore
{
  public static class BitmapUtil
  {
    public static byte[] ToPng(this BitmapSource source)
    {
      using var outStream = new MemoryStream();
      var encoder = new BmpBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(source));
      encoder.Save(outStream);
      
      using var image = Image.FromStream(outStream);
      using var imageStream = new MemoryStream();
            
      image.Save(imageStream, ImageFormat.Png);
      return imageStream.ToArray();
    }
  }
}