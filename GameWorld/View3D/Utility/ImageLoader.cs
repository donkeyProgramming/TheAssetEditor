using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Pfim;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;

namespace GameWorld.Core.Utility
{
    public static class ImageLoader
    {
        private static readonly ILogger _logger = Logging.CreateStatic(typeof(ImageLoader));

        public static Texture2D ForceLoadImage(string fileName, IPackFileService packFileService, GraphicsDevice graphicsDevice, out ImageInformation imageInfo, bool fromFile = false)
        {
            return LoadTextureAsTexture2d(fileName, packFileService, graphicsDevice, out imageInfo, fromFile);
        }

        public static void SaveTexture(Texture2D texture, string path)
        {
            using var stream = new FileStream(path, FileMode.OpenOrCreate);
            texture.SaveAsPng(stream, texture.Width, texture.Height);
        }

        private static byte[] GetFileBytes(IPackFileService packFileService, string fileName, bool fromFile)
        {
            if (fromFile)
            {
                if (File.Exists(fileName) == false)
                {
                    _logger.Here().Error($"Unable to find texture: {fileName}");
                    return null;
                }

                var imageContent = File.ReadAllBytes(fileName);
                return imageContent;
            }
            else
            {
                var imageFile = packFileService.FindFile(fileName);
                if (imageFile == null)
                    return null;
                var imageContent = imageFile.DataSource.ReadData();
                return imageContent;
            }
        }

        public static IImage LoadImageFromBytes(byte[] imageContent, out ImageInformation out_imageInfo)
        {
            using var stream = new MemoryStream(imageContent);
            var image = Pfimage.FromStream(stream);

            out_imageInfo = new ImageInformation();
            out_imageInfo.SetFromImage(image);

            return image;
        }

        private static Texture2D ConvertTexture2D(string fileName, IImage image, GraphicsDevice device)
        {
            Texture2D texture = null;
            if (image.Format == ImageFormat.Rgba32)
            {
                try
                {
                    texture = new Texture2D(device, image.Width, image.Height, true, SurfaceFormat.Bgra32);
                    texture.SetData(0, null, image.Data, 0, image.DataLen);
                }
                catch
                {
                    _logger.Here().Error($"Error loading texture ({fileName} - with format {image.Format}, tried loading as {ImageFormat.Rgba32})");
                }
            }

            if (texture == null)
            {
                _logger.Here().Error($"Error loading texture ({fileName} - Unknown texture format {image.Format})");
                return null;
            }

            // Load mipmaps
            for (var i = 0; i < image.MipMaps.Length; i++)
            {
                try
                {
                    var mipmap = image.MipMaps[i];
                    if (mipmap.Width > 4)
                        texture.SetData(i + 1, null, image.Data, mipmap.DataOffset, mipmap.DataLen);
                }
                catch
                {
                    _logger.Here().Warning($"Error loading Mipmap [{i}]");
                }
            }

            return texture;
        }

        public static Texture2D LoadTextureAsTexture2d(string fileName, IPackFileService pfs, GraphicsDevice device, out ImageInformation out_imageInfo, bool fromFile)
        {
            out_imageInfo = null;
            var imageContent = GetFileBytes(pfs, fileName, fromFile);
            if (imageContent == null)
                return null;

            if (Path.GetExtension(fileName).ToLower() == ".png")
            {
                using var stream = new MemoryStream(imageContent);
                return Texture2D.FromStream(device, stream);
            }

            var image = LoadImageFromBytes(imageContent, out out_imageInfo);
            if (image == null)
                return null;

            return ConvertTexture2D(fileName, image, device);
        }
    }
}
