using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Rendering;

namespace View3D.SceneNodes
{
    public class SimpleDrawableNode : SceneNode, IDrawableItem
    {
        Dictionary<RenderBuckedId, List<IRenderItem>> _renderList = new Dictionary<RenderBuckedId, List<IRenderItem>>();

        public SimpleDrawableNode(string name)
        {
            Name = name;
        }

        public override ISceneNode Clone() => throw new NotImplementedException();

        public void AddItem(RenderBuckedId renderBuckedId, IRenderItem item)
        {
            if (_renderList.ContainsKey(renderBuckedId) == false)
                _renderList[renderBuckedId] = new List<IRenderItem>();

            _renderList[renderBuckedId].Add(item);
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            var m = ModelMatrix * parentWorld;
            foreach (var bucket in _renderList)
            {
                foreach (var item in bucket.Value)
                {
                    item.ModelMatrix = m;
                    renderEngine.AddRenderItem(bucket.Key, item);
                }
            }
           
        }
    }
}
