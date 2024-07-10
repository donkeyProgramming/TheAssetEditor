using System.Collections.Generic;
using GameWorld.Core.Components.Rendering;
using Microsoft.Xna.Framework;

namespace GameWorld.Core.SceneNodes
{
    public class SimpleDrawableNode : GroupNode, IDrawableItem
    {
        readonly Dictionary<RenderBuckedId, List<IRenderItem>> _renderList = new();

        public SimpleDrawableNode(string name)
        {
            Name = name;
        }

        public void AddItem(RenderBuckedId renderBuckedId, IRenderItem item)
        {
            if (_renderList.ContainsKey(renderBuckedId) == false)
                _renderList[renderBuckedId] = [];

            _renderList[renderBuckedId].Add(item);
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            foreach (var bucket in _renderList)
            {
                foreach (var item in bucket.Value)
                {
                    renderEngine.AddRenderItem(bucket.Key, item);
                }
            }
        }

        protected SimpleDrawableNode() { }

        public override ISceneNode CreateCopyInstance() => new SimpleDrawableNode();
    }
}
