using System.Drawing;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.TextureEditor.ViewModels;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Services;
using Shared.GameFormats.Twui.Data;

namespace Editors.Twui.Editor.PreviewRendering
{
    public partial class PreviewRenderer : ObservableObject
    {
        private readonly IWpfGame _wpfGame;
        private readonly TwuiPreviewBuilder _twuiPreviewBuilder;
        TwuiFile? _currentFile;

        [ObservableProperty] ImageSource? _previewImage;

        public PreviewRenderer(IScopedResourceLibrary resourceLibrary, IWpfGame wpfGame, TwuiPreviewBuilder twuiPreviewBuilder)
        {
            _wpfGame = wpfGame;
   
            _twuiPreviewBuilder = twuiPreviewBuilder;
            _wpfGame.ForceEnsureCreated();
        }

        public void SetFile(TwuiFile file)
        {
            _currentFile = file;
            Refresh();
        }

        public void Refresh()
        {
            if (_currentFile == null)
                return;

            // We should get this from the root component
            var width = 1600;
            var height = 900;

            using var renderTarget = new RenderTarget2D(_wpfGame.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);
            _twuiPreviewBuilder.UpdateTexture(renderTarget, _currentFile);

            // Convert it to a format that wpf likes, and add a checkboard background
            using var sourceBitmap = new Bitmap(renderTarget.Width, renderTarget.Height);
            using var g = Graphics.FromImage(sourceBitmap);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            TextureBuilder.DrawCheckerBoard(g, renderTarget.Width, renderTarget.Height);
            var bitmap = TextureBuilder.ConvertTextureToImage(renderTarget);
            g.DrawImage(bitmap, 0, 0);
            
            PreviewImage = TextureBuilder.BitmapToImageSource(sourceBitmap);
        }
    }
}
