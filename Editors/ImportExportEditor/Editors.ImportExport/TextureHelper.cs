using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Pfim;

namespace MeshImportExport
{
    public class TextureHelper
    {
        public static byte[] ConvertDdsToPng(byte[] ddsbyteSteam)
        {
            using var m = new MemoryStream();
            using var w = new BinaryWriter(m);
            w.Write(ddsbyteSteam);
            m.Seek(0, SeekOrigin.Begin);
            var image = Pfimage.FromStream(m);

            PixelFormat pixelFormat = PixelFormat.Format32bppArgb;
            if (image.Format == Pfim.ImageFormat.Rgba32)
            {
                pixelFormat = PixelFormat.Format32bppArgb;
            }
            else if (image.Format == Pfim.ImageFormat.Rgb24)
            {
                pixelFormat = PixelFormat.Format24bppRgb;
            }
            else
            {
                throw new NotSupportedException($"Unsupported DDS format: {image.Format}");
            }

            using var bitmap = new Bitmap(image.Width, image.Height, pixelFormat);
            
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, pixelFormat);
            System.Runtime.InteropServices.Marshal.Copy(image.Data, 0, bitmapData.Scan0, image.DataLen);
            bitmap.UnlockBits(bitmapData);

            using var b = new MemoryStream();
            bitmap.Save(b, System.Drawing.Imaging.ImageFormat.Png);

            using var byteSteam = new BinaryReader(b);
            b.Seek(0, SeekOrigin.Begin);
            var binData = byteSteam.ReadBytes((int)b.Length);
            return binData;
        }

        public static byte[] ConvertPngToDds(byte[] png)
        {
            using var m = new MemoryStream();
            using var w = new BinaryWriter(m);
            w.Write(png);
            m.Seek(0, SeekOrigin.Begin);
            using var bitmap = new Bitmap(m);

            PixelFormat pixelFormat = PixelFormat.Format32bppArgb;
            Pfim.ImageFormat imageFormat = Pfim.ImageFormat.Rgba32;
            if (bitmap.PixelFormat == PixelFormat.Format32bppArgb)
            {
                pixelFormat = PixelFormat.Format32bppArgb;
                imageFormat = Pfim.ImageFormat.Rgba32;
            }
            else if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
            {
                pixelFormat = PixelFormat.Format24bppRgb;
                imageFormat = Pfim.ImageFormat.Rgb24;
            }
            else
            {
                throw new NotSupportedException($"Unsupported PNG format: {bitmap.PixelFormat}");
            }

            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, pixelFormat);
            byte[] imageData = new byte[bitmapData.Stride * bitmapData.Height];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, imageData, 0, imageData.Length);
            bitmap.UnlockBits(bitmapData);

            //var image = Pfim.Pfim.FromStream(//Create(imageData, bitmap.Width, bitmap.Height, imageFormat);

            using var b = new MemoryStream();
            var imageNew = Pfimage.FromStream(b);
            using var writer = new BinaryWriter(b);
            writer.Write(imageNew.Data);
            return b.ToArray();
        }
    }
}
