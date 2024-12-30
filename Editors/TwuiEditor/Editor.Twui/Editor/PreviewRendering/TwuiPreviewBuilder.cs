using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Services;
using Shared.GameFormats.Twui.Data;

namespace Editors.Twui.Editor.PreviewRendering
{
    public class TwuiPreviewBuilder : IDisposable
    {
        private readonly IWpfGame _wpfGame;
        private readonly IScopedResourceLibrary _resourceLibrary;
        SpriteBatch? _spriteBatch;
        Texture2D _whiteSquareTexture;

        public TwuiPreviewBuilder(IWpfGame wpfGame, IScopedResourceLibrary resourceLibrary)
        {
            _wpfGame = wpfGame;
            _resourceLibrary = resourceLibrary;
        }

        SpriteBatch GetSpritebatch()
        {
            if (_spriteBatch == null)
            {
                _spriteBatch = new SpriteBatch(_wpfGame.GraphicsDevice);
                _whiteSquareTexture = new Texture2D(_wpfGame.GraphicsDevice, 1, 1);
                _whiteSquareTexture.SetData([Color.White]);
            }

            return _spriteBatch;
        }

        public void UpdateTexture(RenderTarget2D renderTarget, TwuiFile twuiFile, Component? selectedComponent)
        {
            var device = _wpfGame.GraphicsDevice;
            device.SetRenderTarget(renderTarget);
            device.Clear(Color.Transparent);
            device.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };
           
            var spriteBatch = GetSpritebatch();
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
            DrawHierarchy(Vector2.Zero, twuiFile.Hierarchy.RootItems, twuiFile.Components, selectedComponent);
            spriteBatch.End();

            device.SetRenderTarget(null);
        }

        void DrawHierarchy(Vector2 localSpace, IEnumerable<HierarchyItem> hierarchyItems, List<Component> componentList, Component? selectedComponent)
        {
            foreach (var hierarchyItem in hierarchyItems)
            {
                var component = componentList.FirstOrDefault(x=> hierarchyItem.Id == x.This);
                if (component == null)
                    continue;

                var componentLocalSpace = DrawComponent(localSpace, component, selectedComponent);
                DrawHierarchy(componentLocalSpace, hierarchyItem.Children, componentList, selectedComponent);
            }
        }

        Vector2 DrawComponent(Vector2 localSpace, Component currentComponent, Component? selectedComponent)
        {
            // Take into account docking
            // Take into account colour
            // Take into account state

            var compnentLocalSpace = localSpace + currentComponent.Offset;

            foreach (var image in currentComponent.ComponentImages)
            {
                if (string.IsNullOrWhiteSpace(image.ImagePath))
                    continue;

                var texture = _resourceLibrary.LoadTexture(image.ImagePath);
                if (texture == null)
                    continue;

         
                var compnentWidth = texture.Width;
                var compnentHeight = texture.Height;
                var componentRect = new Rectangle((int)compnentLocalSpace.X, (int)compnentLocalSpace.Y, compnentWidth, compnentHeight );

                _spriteBatch.Draw(texture, componentRect, null, Color.White, 0, Vector2.Zero, SpriteEffects.None, 1);

                // Draw debuginfo about selected component
                if (selectedComponent == currentComponent)
                {
                    var selectionOverlayColour = new Color(255, 0, 0, 50);
                    _spriteBatch.Draw(_whiteSquareTexture, componentRect, null, selectionOverlayColour, 0, Vector2.Zero, SpriteEffects.None, 1);
                    // Draw ancor point
                    // Draw local space point
                }

            }

            return localSpace + currentComponent.Offset;
        }

        public void Dispose()
        {
            _spriteBatch?.Dispose();
        }
    }
}
