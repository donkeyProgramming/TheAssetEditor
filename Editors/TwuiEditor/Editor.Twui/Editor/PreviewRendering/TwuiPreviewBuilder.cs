using Editors.Twui.Editor.Events;
using GameWorld.Core.Components;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Events;
using Shared.Core.Services;
using Shared.GameFormats.Twui.Data;


namespace Editors.Twui.Editor.PreviewRendering
{
    public class TwuiPreviewBuilder : BaseComponent, IDisposable
    {
        private readonly IWpfGame _wpfGame;
        private readonly IScopedResourceLibrary _resourceLibrary;
        private readonly IEventHub _eventHub;

        private RenderTarget2D? _renderTarget;

        SpriteBatch? _spriteBatch;
        Texture2D _whiteSquareTexture;

        public TwuiPreviewBuilder(IWpfGame wpfGame, IScopedResourceLibrary resourceLibrary, IEventHub eventHub)
        {
            _wpfGame = wpfGame;
            _resourceLibrary = resourceLibrary;
            _eventHub = eventHub;
            _eventHub.Register<RedrawTwuiEvent>(this, x => Refresh(x.TwuiFile, x.SelectedComponent));
        }


        public void Initialize()
        {
            _spriteBatch = new SpriteBatch(_wpfGame.GraphicsDevice);
            _whiteSquareTexture = new Texture2D(_wpfGame.GraphicsDevice, 1, 1);
            _whiteSquareTexture.SetData([Color.White]);
        }


        public void Refresh(TwuiFile? twuiFile, Component? selectedComponent)
        {
            if (twuiFile == null)
                return;

            // We should get this from the root component
            var width = 1600;
            var height = 900;

            var renderTarget = GetRenderTarget(width, height);

            UpdateTexture(renderTarget, twuiFile, selectedComponent);
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

        public override void Draw(GameTime gameTime)
        {

            _wpfGame.GraphicsDevice.Clear(Color.Black);
            if (_renderTarget == null)
                return;


            float targetAspectRatio = (float)_renderTarget.Width / _renderTarget.Height;
            float screenAspectRatio = (float)_wpfGame.GraphicsDevice.Viewport.Width / _wpfGame.GraphicsDevice.Viewport.Height;

            Rectangle destinationRectangle;

            if (screenAspectRatio >= targetAspectRatio)
            {
                // Screen is wider than the target
                int width = (int)(_wpfGame.GraphicsDevice.Viewport.Height * targetAspectRatio);
                int x = (_wpfGame.GraphicsDevice.Viewport.Width - width) / 2;
                destinationRectangle = new Rectangle(x, 0, width, _wpfGame.GraphicsDevice.Viewport.Height);
            }
            else
            {
                // Screen is taller than the target
                int height = (int)(_wpfGame.GraphicsDevice.Viewport.Width / targetAspectRatio);
                int y = (_wpfGame.GraphicsDevice.Viewport.Height - height) / 2;
                destinationRectangle = new Rectangle(0, y, _wpfGame.GraphicsDevice.Viewport.Width, height);
            }


            _spriteBatch.Begin();
            var squareSize = 50; // Size of each square
            var boardWidth = 1 + (_wpfGame.GraphicsDevice.Viewport.Width / squareSize);
            var boardHeight = 1 + (_wpfGame.GraphicsDevice.Viewport.Height / squareSize);

            for (int row = 0; row < boardHeight; row++)
            {
                for (int col = 0; col < boardWidth; col++)
                {
                    // Alternate between two colors
                    Color squareColor = (row + col) % 2 == 0 ? Color.Gray : Color.White;

                    // Calculate the position and size of the square
                    Rectangle squareRectangle = new Rectangle(col * squareSize, row * squareSize, squareSize, squareSize);

                    // Draw the square
                    _spriteBatch.Draw(_whiteSquareTexture, squareRectangle, squareColor);
                }
            }

            _spriteBatch.Draw(_renderTarget, destinationRectangle, Color.White);
            _spriteBatch.End();
        }

        void UpdateTexture(RenderTarget2D renderTarget, TwuiFile twuiFile, Component? selectedComponent)
        {
            var device = _wpfGame.GraphicsDevice;
            device.SetRenderTarget(renderTarget);
            device.Clear(Color.Transparent);
            device.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };
           
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
            DrawHierarchy(Vector2.Zero, twuiFile.Hierarchy.RootItems, twuiFile.Components, selectedComponent);
            _spriteBatch.End();

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

        public void Dispose()
        {
            _spriteBatch?.Dispose();
        }
    }
}
