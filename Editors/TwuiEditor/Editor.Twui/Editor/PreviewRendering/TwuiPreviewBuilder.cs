using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Services;
using Shared.GameFormats.Twui.Data;

namespace Editors.Twui.Editor.PreviewRendering
{
    public class TwuiPreviewBuilder
    {
        private readonly IWpfGame _wpfGame;
        private readonly IScopedResourceLibrary _resourceLibrary;
        SpriteBatch _spriteBatch;

        public TwuiPreviewBuilder(IWpfGame wpfGame, IScopedResourceLibrary resourceLibrary)
        {
            _wpfGame = wpfGame;
            _resourceLibrary = resourceLibrary;
        }

        void Create()
        {
            _spriteBatch = new SpriteBatch(_wpfGame.GraphicsDevice);
        }

        void Cleanup()
        {
            _spriteBatch?.Dispose();
        }

        public void UpdateTexture(RenderTarget2D renderTarget, TwuiFile twuiFile)
        {
            Create();
            var device = _wpfGame.GraphicsDevice;
            device.SetRenderTarget(renderTarget);
            device.Clear(Color.Transparent);
            device.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);


            DrawHierarchy(Vector2.Zero, twuiFile.Hierarchy.RootItems, twuiFile.Components);

            device.SetRenderTarget(null);
            Cleanup();
        }

        void DrawHierarchy(Vector2 localSpace, IEnumerable<HierarchyItem> hierarchyItems, List<Component> componentList)
        {
            foreach (var hierarchyItem in hierarchyItems)
            {
                var component = componentList.FirstOrDefault(x=> hierarchyItem.Id == x.This);
                if (component == null)
                    continue;

                var componentLocalSpace = DrawComponent(localSpace, component);
                DrawHierarchy(componentLocalSpace, hierarchyItem.Children, componentList);
            }
        }

        Vector2 DrawComponent(Vector2 localSpace, Component component)
        {
            foreach (var image in component.ComponentImages)
            {
                if (string.IsNullOrWhiteSpace(image.ImagePath))
                    continue;

                var texture = _resourceLibrary.LoadTexture(image.ImagePath);
                if (texture == null)
                    continue;

                _spriteBatch.Draw(texture, component.Offset, Color.White);
            }

            return localSpace;
        }  
    }
}
