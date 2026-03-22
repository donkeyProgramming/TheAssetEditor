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
                combinedBitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
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

            // Pfim returns BGRA data for Rgba32, but Bitmap expects ARGB
            // We need to swap the R and B channels
            byte[] correctedData = new byte[image.DataLen];

            if (image.Format == Pfim.ImageFormat.Rgba32)
            {
                // BGRA -> ARGB conversion
                for (int i = 0; i < image.DataLen; i += 4)
                {
                    correctedData[i] = image.Data[i + 2];     // B -> R
                    correctedData[i + 1] = image.Data[i + 1]; // G -> G
                    correctedData[i + 2] = image.Data[i];     // R -> B
                    correctedData[i + 3] = image.Data[i + 3]; // A -> A
                }
            }
            else if (image.Format == Pfim.ImageFormat.Rgb24)
            {
                // BGR -> RGB conversion
                for (int i = 0; i < image.DataLen; i += 3)
                {
                    correctedData[i] = image.Data[i + 2];     // B -> R
                    correctedData[i + 1] = image.Data[i + 1]; // G -> G
                    correctedData[i + 2] = image.Data[i];     // R -> B
                }
            }
            else
            {
                // For other formats, use the data as-is
                correctedData = image.Data;
            }

            PixelFormat pixelFormat = image.Format == Pfim.ImageFormat.Rgba32 
                ? PixelFormat.Format32bppArgb 
                : PixelFormat.Format24bppRgb;

            var bitmap = new Bitmap(image.Width, image.Height, pixelFormat);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, pixelFormat);
            System.Runtime.InteropServices.Marshal.Copy(correctedData, 0, bitmapData.Scan0, correctedData.Length);
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }
    }
}
