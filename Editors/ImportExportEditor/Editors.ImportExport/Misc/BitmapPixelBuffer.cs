using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Editors.ImportExport.Misc;

/// <summary>
/// Reads and writes 32-bit GDI+ bitmaps without the per-pixel overhead of
/// Bitmap.GetPixel/SetPixel. Format32bppArgb is laid out as BGRA in memory.
/// </summary>
internal static class BitmapPixelBuffer
{
    private const PixelFormat PixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb;

    public static byte[] ReadBgra(Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        var width = bitmap.Width;
        var height = bitmap.Height;
        var rowBytes = checked(width * 4);
        var pixels = new byte[checked(rowBytes * height)];
        var rectangle = new Rectangle(0, 0, width, height);
        var bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat);
        try
        {
            CopyRows(bitmapData, pixels, rowBytes, height, toBitmap: false);
            return pixels;
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    public static Bitmap CreateBitmap(int width, int height, byte[] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);

        var rowBytes = checked(width * 4);
        if (pixels.Length < checked(rowBytes * height))
            throw new ArgumentException("The pixel buffer is smaller than the requested bitmap.", nameof(pixels));

        var bitmap = new Bitmap(width, height, PixelFormat);
        try
        {
            var rectangle = new Rectangle(0, 0, width, height);
            var bitmapData = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat);
            try
            {
                CopyRows(bitmapData, pixels, rowBytes, height, toBitmap: true);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    public static void WriteBgra(Bitmap bitmap, byte[] pixels)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(pixels);

        var width = bitmap.Width;
        var height = bitmap.Height;
        var rowBytes = checked(width * 4);
        if (pixels.Length < checked(rowBytes * height))
            throw new ArgumentException("The pixel buffer is smaller than the bitmap.", nameof(pixels));

        var rectangle = new Rectangle(0, 0, width, height);
        var bitmapData = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat);
        try
        {
            CopyRows(bitmapData, pixels, rowBytes, height, toBitmap: true);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    private static void CopyRows(
        BitmapData bitmapData,
        byte[] pixels,
        int rowBytes,
        int height,
        bool toBitmap)
    {
        var stride = bitmapData.Stride;
        for (var row = 0; row < height; row++)
        {
            var physicalRow = stride >= 0 ? row : height - 1 - row;
            var address = IntPtr.Add(bitmapData.Scan0, checked(physicalRow * stride));
            var bufferOffset = checked(row * rowBytes);

            if (toBitmap)
                Marshal.Copy(pixels, bufferOffset, address, rowBytes);
            else
                Marshal.Copy(address, pixels, bufferOffset, rowBytes);
        }
    }
}
