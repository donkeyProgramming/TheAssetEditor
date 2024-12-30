using GameWorld.Core.Services;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.Twui.Data;

namespace Editors.Twui.Editor.PreviewRendering
{
    public class TwuiPreviewBuilder
    {
        private readonly IWpfGame _wpfGame;
        private readonly IScopedResourceLibrary _resourceLibrary;
        private readonly IPackFileService _packFileService;

        public TwuiPreviewBuilder(IWpfGame wpfGame, IScopedResourceLibrary resourceLibrary, IPackFileService packFileService)
        {
            _wpfGame = wpfGame;
            _resourceLibrary = resourceLibrary;
            _packFileService = packFileService;
        }

        public void UpdateTexture(RenderTarget2D renderTarget, TwuiFile _currentFile)
        {
            var textures = _currentFile.Components
                .SelectMany(x => x.ComponentImages)
                .Select(x => x.ImagePath)
                .Distinct()
                .ToList();

            var device = _wpfGame.GraphicsDevice;
            var spriteBatch = new SpriteBatch(device);   

            device.SetRenderTarget(renderTarget);
            device.Clear(Microsoft.Xna.Framework.Color.Transparent);
            device.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);

            var notFound = new List<string>();

            var componentList = _currentFile.Components.OrderByDescending(x => x.Priority).ToList();
            foreach (var comp in _currentFile.Components)
            {
                foreach (var image in comp.ComponentImages)
                {
                    if (string.IsNullOrWhiteSpace(image.ImagePath))
                        continue;

                    var found = _packFileService.FindFile(image.ImagePath);
                    if (found == null)
                    {
                        notFound.Add(image.ImagePath);
                        continue;
                    }

                    var texture = _resourceLibrary.ForceLoadImage(image.ImagePath, out var imageInformation);
                    spriteBatch.Draw(texture, comp.Offset, Microsoft.Xna.Framework.Color.White);

                }
            }

            device.SetRenderTarget(null);
            spriteBatch?.Dispose();
        }
    
    }
}
