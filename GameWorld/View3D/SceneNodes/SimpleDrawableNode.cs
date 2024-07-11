using System.Collections.Generic;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.RenderItems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.SceneNodes
{
    // This whole class is very hacky. Please refactor at some points!

    public class SimpleDrawableNode : GroupNode, IDrawableItem
    {
        private readonly List<WorldTextRenderItem> _renderList = [];
        private readonly List<VertexPositionColor> _lineVertexList = [];

        public SimpleDrawableNode(string name)
        {
            Name = name;
        }

        public void AddItem(WorldTextRenderItem item)
        {
            _renderList.Add(item);
        }

        public void AddItem(VertexPositionColor[] lineArray)
        {
            _lineVertexList.AddRange(lineArray);
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            var m = ModelMatrix * parentWorld;
            foreach (var item in _renderList)
            {
                item.ModelMatrix = m;
                renderEngine.AddRenderItem(RenderBuckedId.Font, item);
            }

            for (var i = 0; i < _lineVertexList.Count; i += 2)
            {
                var transformedPos0 = Vector3.Transform(_lineVertexList[i+0].Position, m);
                var transformed0 = new VertexPositionColor(transformedPos0, _lineVertexList[i+0].Color);

                var transformedPos1 = Vector3.Transform(_lineVertexList[i + 1].Position, m);
                var transformed1 = new VertexPositionColor(transformedPos1, _lineVertexList[i + 1].Color);

                renderEngine.AddRenderLines([transformed0, transformed1]);
            }
        }

        protected SimpleDrawableNode() { }

        public override ISceneNode CreateCopyInstance() => new SimpleDrawableNode();
    }
}
