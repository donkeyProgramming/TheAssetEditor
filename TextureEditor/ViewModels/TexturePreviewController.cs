using CommonControls.BaseDialogs;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using TextureEditor.Views;
using View3D.Scene;
using View3D.Utility;

namespace TextureEditor.ViewModels
{
    public class TexturePreviewController : IDisposable
    {
        PackFileService _packFileService;
        ResourceLibary _resourceLib;
        TextureToTextureRenderer _textureRenderer;
        string _imagePath;
        TexturePreviewViewModel _viewModel;
        SceneContainer _scene;

        public static void CreateWindow(string imagePath, PackFileService packFileService)
        {
            TexturePreviewViewModel viewModel = new TexturePreviewViewModel();
            viewModel.ImagePath.Value = imagePath;

            using (var controller = new TexturePreviewController(imagePath, viewModel, packFileService))
            {
                var containingWindow = new ControllerHostWindow(false, ResizeMode.CanResize);
                containingWindow.Title = "Texture Preview Window";
                containingWindow.Content = new TexturePreviewView() { DataContext = viewModel };
                containingWindow.ShowDialog();
            }
        }

        public static void CreateFromFilePath(string imageDiskPath, PackFileService packFileService)
        {
            try
            {
                if (packFileService.HasEditablePackFile() == false)
                    return;

                var directoryPath = Path.GetDirectoryName(imageDiskPath);
                var justFileName = Path.GetFileName(imageDiskPath);
                var temp_packFile = new PackFile(justFileName, new FileSystemSource(imageDiskPath));
                packFileService.AddFileToPack(packFileService.GetEditablePack(), Path.GetDirectoryName(imageDiskPath), temp_packFile);

                TexturePreviewViewModel viewModel = new TexturePreviewViewModel();
                viewModel.ImagePath.Value = imageDiskPath;

                using (var controller = new TexturePreviewController(imageDiskPath, viewModel, packFileService))
                {
                    var containingWindow = new ControllerHostWindow(false, ResizeMode.CanResize);
                    containingWindow.Title = "Texture Preview Window";
                    containingWindow.Content = new TexturePreviewView() { DataContext = viewModel };
                    containingWindow.ShowDialog();
                }

                packFileService.DeleteFile(packFileService.GetEditablePack(), temp_packFile);
            }
            catch (Exception e)
            { 
                
            }
        }


        public TexturePreviewController(string imagePath, TexturePreviewViewModel viewModel, PackFileService packFileService)
        {
            _imagePath = imagePath;
            _viewModel = viewModel;
            _packFileService = packFileService;

            _scene = new SceneContainer();
            _scene.Components.Add(new ResourceLibary(_scene, packFileService ));
            _scene.ForceCreate();

            _resourceLib = _scene.GetComponent<ResourceLibary>();
            _textureRenderer = new TextureToTextureRenderer(_scene.GraphicsDevice, new SpriteBatch(_scene.GraphicsDevice), _resourceLib);
            CreateImage();
        }


        void CreateImage()
        {
            var texture = _resourceLib.ForceLoadImage(_imagePath, out var imageInformation);
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
