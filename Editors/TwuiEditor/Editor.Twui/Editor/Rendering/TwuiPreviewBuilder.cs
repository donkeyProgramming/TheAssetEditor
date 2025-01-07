using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Services;
using Shared.GameFormats.Twui.Data;

//https://github.com/Apostolique/Apos.Gui/blob/main/Source/Dock.cs



namespace Editors.Twui.Editor.Rendering
{
    public class TwuiPreviewBuilder
    {
        private readonly IWpfGame _wpfGame;
        private readonly IScopedResourceLibrary _resourceLibrary;

        private RenderTarget2D _renderTarget;
        private SpriteBatch _spriteBatch;
        private Texture2D _whiteSquareTexture;

        public TwuiPreviewBuilder(IWpfGame wpfGame, IScopedResourceLibrary resourceLibrary)
        {
            _wpfGame = wpfGame;
            _resourceLibrary = resourceLibrary;
        }

        public void Initialize()
        {
            _spriteBatch = new SpriteBatch(_wpfGame.GraphicsDevice);
            _whiteSquareTexture = new Texture2D(_wpfGame.GraphicsDevice, 1, 1);
            _whiteSquareTexture.SetData([Color.White]);
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

        public RenderTarget2D UpdateTexture(TwuiFile twuiFile, Component? selectedComponent)
        {
            // We should get this from the root component
            var width = 1600;
            var height = 900;
            var renderTarget = GetRenderTarget(width, height);

            var device = _wpfGame.GraphicsDevice;
            device.SetRenderTarget(_renderTarget);
            device.Clear(Color.Transparent);
            device.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
            DrawHierarchy(Vector2.Zero, twuiFile.Hierarchy.RootItems, twuiFile.Components, selectedComponent);
            _spriteBatch.End();

            device.SetRenderTarget(null);

            return _renderTarget;
        }

        void DrawHierarchy(Vector2 localSpace, IEnumerable<HierarchyItem> hierarchyItems, List<Component> componentList, Component? selectedComponent)
        {
            foreach (var hierarchyItem in hierarchyItems)
            {
                var component = componentList.FirstOrDefault(x => hierarchyItem.Id == x.This);
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

            var computedLocalSpace = ComponentCoordinateHelper.GetLocalCoordinateSpace(currentComponent, Rectangle.Empty);
            var compnentLocalSpace = localSpace + computedLocalSpace;

            foreach (var image in currentComponent.ComponentImages)
            {
                if (string.IsNullOrWhiteSpace(image.ImagePath))
                    continue;

                var texture = _resourceLibrary.LoadTexture(image.ImagePath);
                if (texture == null)
                    continue;

                var compnentWidth = texture.Width;
                var compnentHeight = texture.Height;
                var componentRect = new Rectangle((int)compnentLocalSpace.X, (int)compnentLocalSpace.Y, compnentWidth, compnentHeight);

                _spriteBatch.Draw(texture, componentRect, null, new Color(255, 255, 255, 255), 0, Vector2.Zero, SpriteEffects.None, 1);

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

        void DrawComponentSpaceOutline(Rectangle componentRectable)
        { 
        
        }

    }
}
