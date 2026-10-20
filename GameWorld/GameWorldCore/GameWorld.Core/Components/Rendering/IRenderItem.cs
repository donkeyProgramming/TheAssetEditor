using GameWorld.Core.Rendering;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Components.Rendering
{
    public enum RenderBuckedId
    {
        Normal,
        Wireframe,
        Selection,
        Font,
        Texture2D,
        Grid,
        Transparent,
    }

    public enum RenderingTechnique
    {
        Normal,
        Emissive,
        /// <summary>The view depth of what is drawn, for soft particles to fade against; drawn only by what supports it.</summary>
        Depth,
    }

    enum RasterizerStateEnum
    {
        Normal,
        Wireframe,
        SelectedFaces,
    }

    public interface IRenderItem
    {
        void Draw(GraphicsDevice device, CommonShaderParameters parameters, RenderingTechnique renderingTechnique);
    }
}
