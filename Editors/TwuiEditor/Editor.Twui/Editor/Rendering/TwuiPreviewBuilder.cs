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
            device.SetRenderTarget(renderTarget);
            device.Clear(Color.Transparent);
            device.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };

            //_spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);//, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            DrawHierarchy(Rectangle.Empty, twuiFile.Hierarchy.RootItems, twuiFile.Components, selectedComponent, 0);
            _spriteBatch.End();

            device.SetRenderTarget(null);

            return renderTarget;
        }

        void DrawHierarchy(Rectangle localSpace, IEnumerable<HierarchyItem> hierarchyItems, List<Component> componentList, Component? selectedComponent, int depth)
        {
            var hierarchyItems2 = hierarchyItems
                .Select(x => (x, componentList.FirstOrDefault(y => x.Id == y.This)))
                .Where(x=>x.Item2 != null)
                .OrderByDescending(x => x.Item2.Priority)
                .ToList();
               
            foreach (var hierarchyItem in hierarchyItems2)
            {
                var component = componentList.FirstOrDefault(x => hierarchyItem.x.Id == x.This);
                if (component == null)
                    continue;


                var componentLocalSpace = DrawComponent(localSpace, component, selectedComponent, depth, hierarchyItem.x.IsVisible);


                var width = 0; var height = 0;
                var currentStateId = component.Currentstate;
                var currentState = component.States.FirstOrDefault(x => x.UniqueGuid == currentStateId);
                if (currentState != null)
                {
                    width = (int)currentState.Width;
                    height = (int)currentState.Height;
                }

                //var spacing = new string('\t', depth);
                //Console.WriteLine($"{spacing}{component.Name}: Offset:{component.Offset}, Width:{width}, Height:{height}, Rect:{componentLocalSpace} -- DockingX:{component.DockingHorizontal}, DockingY:{component.DockingVertical}, DockOffset:{component.Dock_offset}, Acor:{component.Component_anchor_point}");

                DrawHierarchy(componentLocalSpace, hierarchyItem.x.Children, componentList, selectedComponent, depth+1);
            }
        }

        Rectangle DrawComponent(Rectangle localSpace, Component currentComponent, Component? selectedComponent, int depth, bool isVisible)
        {



            var invMaxLayerDepth = 1f/999999f;

            // Take into account docking
            // Take into account colour
            // Take into account state

            // Get the state 

            var compnentLocalSpace = ComponentCoordinateHelper.GetComponentStateLocalCoordinateSpace(currentComponent, localSpace);

            if (currentComponent.Name == "page_cycle")
            { 
            }




            var currentStateId = currentComponent.Currentstate;
            var currentState = currentComponent.States.FirstOrDefault(x => x.UniqueGuid == currentStateId);
            if (currentState != null)
            {
                foreach (var stateImage in currentState.Images)
                {
                    var imageId = stateImage.Componentimage;
                    var image = currentComponent.ComponentImages.FirstOrDefault(x=>x.This == imageId);
                    if (image == null)
                        continue;


                    if (string.IsNullOrWhiteSpace(image.ImagePath))
                        continue;

                    if (image.ImagePath.Contains("panel_back_tile.png"))
                        continue;

                    var texture = _resourceLibrary.LoadTexture(image.ImagePath);
                    if (texture == null)
                        continue;


                    if (isVisible)
                    {
                        var imageLocalSpace = ComponentCoordinateHelper.GetComponentStateImageLocalCoordinateSpace(stateImage, compnentLocalSpace);
                        var pri = currentComponent.Priority * invMaxLayerDepth;
                        _spriteBatch.Draw(texture, imageLocalSpace, null, new Color(255, 255, 255, 255), 0, Vector2.Zero, SpriteEffects.None, pri);
                    }
                }
            }

            if (selectedComponent == currentComponent)
            {
                var selectionOverlayColour = new Color(255, 0, 0, 50);
                //_spriteBatch.Draw(_whiteSquareTexture, compnentLocalSpace, null, selectionOverlayColour, 0, Vector2.Zero, SpriteEffects.None, 1);
                _spriteBatch.Draw(_whiteSquareTexture, compnentLocalSpace, null, selectionOverlayColour, 0, Vector2.Zero, SpriteEffects.None, 1);

                // Draw ancor point
                // Draw local space point
            }

            //foreach (var image in currentComponent.ComponentImages)
            //{
            //
            // 
            //
            //    if (string.IsNullOrWhiteSpace(image.ImagePath))
            //        continue;
            //
            //    var texture = _resourceLibrary.LoadTexture(image.ImagePath);
            //    if (texture == null)
            //        continue;
            //
            //    var compnentWidth = texture.Width;
            //    var compnentHeight = texture.Height;
            //    var componentRect = new Rectangle((int)compnentLocalSpace.X, (int)compnentLocalSpace.Y, compnentWidth, compnentHeight);
            //
            //
            //    // Give the component rect, modify with the imagemetrics/image attribuete for each image
            //
            //    var pri = currentComponent.Priority * invMaxLayerDepth;
            //    _spriteBatch.Draw(texture, componentRect, null, new Color(255, 255, 255, 255), 0, Vector2.Zero, SpriteEffects.None, pri);
            //
            //
            //
            //
            //    // Draw red line around componet
            //    // Draw green line around images 
            //}
            //
            //
            //
            return compnentLocalSpace;
        }

        void DrawComponentSpaceOutline(Rectangle componentRectable)
        { 
        
        }

    }
}
