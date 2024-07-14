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
    }

    public enum RenderingTechnique
    {
        Normal,
        Emissive,
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
