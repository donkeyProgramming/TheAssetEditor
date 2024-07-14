using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Components.Rendering
{
    internal class RenderTargetHelper
    {
        public static RenderTarget2D GetRenderTarget(GraphicsDevice device, RenderTarget2D existingRenderTarget)
        {
            var width = device.Viewport.Width;
            var height = device.Viewport.Height;

            if (existingRenderTarget == null)
            {
                return new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);
            }

            if (existingRenderTarget.Width == width && existingRenderTarget.Height == height)
                return existingRenderTarget;

            existingRenderTarget.Dispose();
            return new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);
        }
    }
}
