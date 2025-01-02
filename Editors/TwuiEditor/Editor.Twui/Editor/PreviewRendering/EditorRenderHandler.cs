using System.Drawing;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Editors.TextureEditor.ViewModels;
using Editors.Twui.Editor.Events;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Events;
using Shared.Core.Services;
using Shared.GameFormats.Twui.Data;

namespace Editors.Twui.Editor.PreviewRendering
{
    public partial class EditorRenderHandler : ObservableObject, IDisposable
    {
        private readonly IEventHub _eventHub;
        private readonly IWpfGame _wpfGame;
        private readonly TwuiPreviewBuilder _twuiPreviewBuilder;

        private RenderTarget2D? _renderTarget;

        [ObservableProperty]public partial ImageSource? PreviewImage { get; set; }

        public EditorRenderHandler(IEventHub eventHub, IWpfGame wpfGame, TwuiPreviewBuilder twuiPreviewBuilder)
        {
            _eventHub = eventHub;
            _wpfGame = wpfGame;
   
            _twuiPreviewBuilder = twuiPreviewBuilder;
            _wpfGame.ForceEnsureCreated();

            _eventHub.Register<RedrawTwuiEvent>(this, x => Refresh(x.TwuiFile, x.SelectedComponent));
        }

        public void Refresh(TwuiFile? twuiFile, Component? selectedComponent)
        {
            if (twuiFile == null)
                return;

            // We should get this from the root component
            var width = 1600;
            var height = 900;

            var renderTarget = GetRenderTarget(width, height);
            _twuiPreviewBuilder.UpdateTexture(renderTarget, twuiFile, selectedComponent);

            // Convert it to a format that wpf likes, and add a checkboard background
            using var sourceBitmap = new Bitmap(renderTarget.Width, renderTarget.Height);
            using var g = Graphics.FromImage(sourceBitmap);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            TextureBuilder.DrawCheckerBoard(g, renderTarget.Width, renderTarget.Height);
            var bitmap = TextureBuilder.ConvertTextureToImage(renderTarget);
            g.DrawImage(bitmap, 0, 0);
            
            PreviewImage = TextureBuilder.BitmapToImageSource(sourceBitmap);
        }

        RenderTarget2D GetRenderTarget(int width, int height)
        {
            var reCreateRenderTarget = false;
            reCreateRenderTarget = _renderTarget == null;
            // Check that size is same as before

            if (reCreateRenderTarget)
            {
                _renderTarget?.Dispose();
                _renderTarget = new RenderTarget2D(_wpfGame.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);
            }

            return _renderTarget!;
        }

        public void Dispose()
        {
            _eventHub?.UnRegister(this);
            _renderTarget?.Dispose();
        }
    }
}
