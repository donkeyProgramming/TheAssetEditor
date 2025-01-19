using System.ComponentModel.DataAnnotations;
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
        private readonly float _invMaxLayerDepth = 1f / 999999f;

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

        public RenderTarget2D UpdateTexture(TwuiContext twuiContext)
        {
            // We should get this from the root component
            var width = 1600;
            var height = 900;
            var renderTarget = GetRenderTarget(width, height);

            var device = _wpfGame.GraphicsDevice;
            device.SetRenderTarget(renderTarget);
            device.Clear(Color.Transparent);
            device.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };

            //_spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);//, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            var debugData = new List<DebugData>();
            DrawHierarchy(Rectangle.Empty, twuiContext.Componenets, 0, ref debugData);


            foreach(var debug in debugData)
                _spriteBatch.Draw(_whiteSquareTexture, debug.renderRect, null, debug.color, 0, Vector2.Zero, SpriteEffects.None, 1);

            _spriteBatch.End();

            device.SetRenderTarget(null);

            return renderTarget;
        }

        void DrawHierarchy(Rectangle localSpace, IEnumerable<TwuiComponent> components, int depth, ref List<DebugData> debugData)
        {
            var sortedComponents = components.OrderByDescending(x=>x.Priority).ToList();
            foreach (var component in sortedComponents)
            {
                var componentLocalSpace = DrawComponent(localSpace, component, depth, ref debugData);
                DrawHierarchy(componentLocalSpace, component.Children, depth+1, ref debugData);
            }
        }

        Rectangle DrawComponent(Rectangle localSpace, TwuiComponent component, int depth, ref List<DebugData> debugData)
        {
            var compnentLocalSpace = ComponentCoordinateHelper.GetComponentStateLocalCoordinateSpace(component, localSpace);

            if (component.Name == "round_small_button")
            { 
            }

            var currentState = component.CurrentState;
            if (currentState != null)
            {
                foreach (var image in currentState.ImageList)
                {
                    if (image == null || string.IsNullOrWhiteSpace(image.Path))
                        continue;

                    var texture = _resourceLibrary.LoadTexture(image.Path);
                    if (texture == null)
                        continue;

                    if (component.ShowInPreviewRenderer)
                    {
                        var imageLocalSpace = ComponentCoordinateHelper.GetComponentStateImageLocalCoordinateSpace(image, compnentLocalSpace, component.Location.Component_anchor_point);
                        var pri = component.Priority * _invMaxLayerDepth;
                        _spriteBatch.Draw(texture, imageLocalSpace, null, new Color(255, 255, 255, 255), 0, Vector2.Zero, SpriteEffects.None, pri);
                        if (component.IsSelected)
                        {
                            var selectionOverlayColour = new Color(0, 250, 0, 50);
                            debugData.Add(new DebugData(imageLocalSpace, selectionOverlayColour));
                        }
                    }
                }
            }

            if (component.IsSelected)
            {
                var selectionOverlayColour = new Color(255, 0, 0, 50);
                debugData.Add(new DebugData(compnentLocalSpace, selectionOverlayColour));
            }

            return compnentLocalSpace;
        }

        record DebugData(Rectangle renderRect, Color color);
    }
}
