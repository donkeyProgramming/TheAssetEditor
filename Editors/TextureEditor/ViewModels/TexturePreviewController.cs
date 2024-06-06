using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using GameWorld.WpfWindow;
using Microsoft.Xna.Framework.Graphics;
using Monogame.WpfInterop.ResourceHandling;
using View3D.Rendering;

namespace TextureEditor.ViewModels
{
    public class TexturePreviewController : IDisposable
    {
        private readonly ResourceLibrary _resourceLib;
        private readonly TextureToTextureRenderer _textureRenderer;
        private readonly TexturePreviewViewModel _viewModel;
        private readonly WpfGame _scene;

        public TexturePreviewController(TexturePreviewViewModel viewModel, ResourceLibrary resourceLibrary, WpfGame wpfGame)
        {
            _viewModel = viewModel;

            _resourceLib = resourceLibrary;
            _scene = wpfGame;

            _textureRenderer = new TextureToTextureRenderer(_scene.GraphicsDevice, new SpriteBatch(_scene.GraphicsDevice), _resourceLib);
        }

        public void Build(string imagePath)
        {
            CreateImage(imagePath);
        }

        void CreateImage(string imagePath)
        {
            var texture = _resourceLib.ForceLoadImage(imagePath, out var imageInformation);
            _viewModel.SetImageInformation(imageInformation);

            var imageGenerationSettings = new TextureToTextureRenderer.DrawSettings[5];
            imageGenerationSettings[0] = new TextureToTextureRenderer.DrawSettings();
            imageGenerationSettings[1] = new TextureToTextureRenderer.DrawSettings() { OnlyRed = true };
            imageGenerationSettings[2] = new TextureToTextureRenderer.DrawSettings() { OnlyBlue = true };
            imageGenerationSettings[3] = new TextureToTextureRenderer.DrawSettings() { OnlyGreen = true };
            imageGenerationSettings[4] = new TextureToTextureRenderer.DrawSettings() { OnlyAlpha = true };

            for (int i = 0; i < imageGenerationSettings.Count(); i++)
            {
                using (var renderedTexture = _textureRenderer.RenderToTexture(texture, texture.Width, texture.Height, imageGenerationSettings[i]))
                {
                    using (var sourceBitmap = new Bitmap(texture.Width, texture.Height))
                    {
                        using (var g = Graphics.FromImage(sourceBitmap))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            DrawCheckerBoard(g, texture.Width, texture.Height);
                            var bitmap = ConvertTextureToImage(renderedTexture);
                            g.DrawImage(bitmap, 0, 0);
                            _viewModel.PreviewImage[i] = BitmapToImageSource(sourceBitmap);
                        }
                    }
                }
            }

            _viewModel.FormatRgbaCheckbox = true;
        }

        private void DrawCheckerBoard(Graphics g, int width, int height)
        {
            var size = 50;
            var countX = (width / size) + 1;
            var countY = (height / size) + 1;
            using (SolidBrush blackBrush = new SolidBrush(Color.DarkGray))
            using (SolidBrush whiteBrush = new SolidBrush(Color.LightGray))
            {
                for (int i = 0; i < countX; i++)
                {
                    for (int j = 0; j < countY; j++)
                    {
                        if ((j % 2 == 0 && i % 2 == 0) || (j % 2 != 0 && i % 2 != 0))
                            g.FillRectangle(blackBrush, i * size, j * size, size, size);
                        else if ((j % 2 == 0 && i % 2 != 0) || (j % 2 != 0 && i % 2 == 0))
                            g.FillRectangle(whiteBrush, i * size, j * size, size, size);
                    }
                }
            }
        }

        public Image ConvertTextureToImage(Texture2D texture)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                texture.SaveAsPng(stream, texture.Width, texture.Height);
                stream.Seek(0, SeekOrigin.Begin);
                return Image.FromStream(stream);
            }
        }

        BitmapImage BitmapToImageSource(Image bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public void Dispose()
        {
            _textureRenderer.Dispose();
            _scene.Dispose();
        }
    }
}
