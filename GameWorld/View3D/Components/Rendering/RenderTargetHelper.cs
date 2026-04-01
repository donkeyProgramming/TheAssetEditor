using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Components.Rendering
{
    internal class RenderTargetHelper
    {
        public static RenderTarget2D GetRenderTarget(GraphicsDevice device, RenderTarget2D existingRenderTarget, float imageUpScale)
        {
            var width = (int)(device.Viewport.Width * imageUpScale);
            var height = (int)(device.Viewport.Height * imageUpScale);

            if (existingRenderTarget == null)
            {
                return new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.PreserveContents);
            }

            if (existingRenderTarget.Width == width && existingRenderTarget.Height == height)
                return existingRenderTarget;

            existingRenderTarget.Dispose();
            return new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.PreserveContents);
        }
    }
}
