using GameWorld.Core.Components.Rendering;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.Vfx
{
    /// <summary>Queues an effect's particles in the transparent pass every frame. IDisposable, because the scene
    /// disposes its nodes when the editor closes and reaches only the ones that are.</summary>
    public class VfxEffectNode : GroupNode, IDrawableItem, IDisposable
    {
        public ParticleRenderItem RenderItem { get; }

        public VfxEffectNode(string name, ParticleRenderItem renderItem) : base(name) => RenderItem = renderItem;

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            RenderItem.World = ModelMatrix * parentWorld;
            renderEngine.AddRenderItem(RenderBuckedId.Transparent, RenderItem);
        }

        public void Dispose() => RenderItem.Dispose();
    }
}
