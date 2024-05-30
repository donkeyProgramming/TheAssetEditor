using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using CommonControls.BaseDialogs;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using TextureEditor.Views;
using View3D.Rendering;
using View3D.Services;
using View3D.Utility;

namespace TextureEditor.ViewModels
{
    public class ViewModelWrapper : NotifyPropertyChangedImpl
    {
        TexturePreviewViewModel _viewModel;
        public TexturePreviewViewModel ViewModel
        {
            get => _viewModel;
            set => SetAndNotify(ref _viewModel, value);
        }

        public void ShowTextureDetailsInfo() => ViewModel.ShowTextureDetailsInfo();
    }

    public static class TexturePreviewControllerCreator
    {
        public static void CreateWindow(string imagePath, PackFileService packFileService)
        {
            TexturePreviewViewModel viewModel = new TexturePreviewViewModel();
            viewModel.ImagePath.Value = imagePath;

            using (var controller = new TexturePreviewController(imagePath, viewModel, packFileService))
            {
                var containingWindow = new ControllerHostWindow(false, ResizeMode.CanResize);
                containingWindow.Title = "Texture Preview Window";
                containingWindow.Content = new TexturePreviewView() { DataContext = new ViewModelWrapper() { ViewModel = viewModel } };
                containingWindow.ShowDialog();
            }
        }
    }

    public class TexturePreviewController : IDisposable
    {
        private readonly PackFileService _packFileService;
        private readonly ResourceLibary _resourceLib;
        private readonly TextureToTextureRenderer _textureRenderer;
        private readonly string _imagePath;
        private readonly TexturePreviewViewModel _viewModel;
        private readonly GameWorld _scene;

        public TexturePreviewController(string imagePath, TexturePreviewViewModel viewModel, PackFileService packFileService)
        {
            _imagePath = imagePath;
            _viewModel = viewModel;
            _packFileService = packFileService;

            _scene = new GameWorld(null);
            _scene.Components.Add(new ResourceLibary(_scene, packFileService));
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
                            TextureHelper.DrawCheckerBoard(g, texture.Width, texture.Height);
                            var bitmap = TextureHelper.ConvertTextureToImage(renderedTexture);
                            g.DrawImage(bitmap, 0, 0);
                            _viewModel.PreviewImage[i] = TextureHelper.BitmapToImageSource(sourceBitmap);
                        }
                    }
                }
            }

            _viewModel.FormatRgbaCheckbox = true;
        }


        public void Dispose()
        {
            _textureRenderer.Dispose();
            _scene.Dispose();
        }
    }
}
