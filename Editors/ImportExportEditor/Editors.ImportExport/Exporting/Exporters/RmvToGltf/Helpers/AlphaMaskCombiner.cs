using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Pfim;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers
{
    /// <summary>
    /// Combines a mask texture with a diffuse texture to create a single RGBA texture
    /// where the mask becomes the alpha channel.
    /// </summary>
    public class AlphaMaskCombiner
    {
        /// <summary>
        /// Combines a diffuse (RGB) and mask (Grayscale) into a single RGBA texture.
        /// </summary>
        public static byte[] CombineDiffuseWithMask(byte[] diffuseDdsBytes, byte[] maskDdsBytes)
        {
            try
            {
                // Convert both DDS to bitmaps
                var diffuseBitmap = ConvertDdsToBitmap(diffuseDdsBytes);
                var maskBitmap = ConvertDdsToBitmap(maskDdsBytes);

                // Ensure same dimensions
                if (diffuseBitmap.Width != maskBitmap.Width || diffuseBitmap.Height != maskBitmap.Height)
                {
                    throw new InvalidOperationException(
                        $"Diffuse and mask textures have different dimensions: " +
                        $"diffuse {diffuseBitmap.Width}x{diffuseBitmap.Height}, " +
                        $"mask {maskBitmap.Width}x{maskBitmap.Height}");
                }

                // Create RGBA bitmap
                using var combinedBitmap = new Bitmap(diffuseBitmap.Width, diffuseBitmap.Height, PixelFormat.Format32bppArgb);
                
                // Combine pixels
                for (int x = 0; x < diffuseBitmap.Width; x++)
                {
                    for (int y = 0; y < diffuseBitmap.Height; y++)
                    {
                        var diffusePixel = diffuseBitmap.GetPixel(x, y);
                        var maskPixel = maskBitmap.GetPixel(x, y);

                        // Use mask's grayscale value as alpha
                        int alpha = maskPixel.R;  // Grayscale uses same value for R, G, B
                        var combinedPixel = Color.FromArgb(alpha, diffusePixel.R, diffusePixel.G, diffusePixel.B);
                        
                        combinedBitmap.SetPixel(x, y, combinedPixel);
                    }
                }

                diffuseBitmap.Dispose();
                maskBitmap.Dispose();

                // Export to PNG with alpha
                using var pngStream = new MemoryStream();
                combinedBitmap.Save(pngStream, ImageFormat.Png);
                return pngStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to combine diffuse and mask textures: {ex.Message}", ex);
            }
        }

        private static Bitmap ConvertDdsToBitmap(byte[] ddsBytes)
        {
            using var m = new MemoryStream();
            using var w = new BinaryWriter(m);
            w.Write(ddsBytes);
            m.Seek(0, SeekOrigin.Begin);
            
            var image = Pfimage.FromStream(m);
            
            PixelFormat pixelFormat = image.Format == Pfim.ImageFormat.Rgba32 
                ? PixelFormat.Format32bppArgb 
                : PixelFormat.Format24bppRgb;

            var bitmap = new Bitmap(image.Width, image.Height, pixelFormat);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, pixelFormat);
            System.Runtime.InteropServices.Marshal.Copy(image.Data, 0, bitmapData.Scan0, image.DataLen);
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }
    }
}
