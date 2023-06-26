using CommonControls.Common;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.RenderItems;
using View3D.Utility;

namespace View3D.Components.Component
{
    public class GridComponent : BaseComponent, IDisposable
    {
        LineMeshRender _gridMesh;
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly ResourceLibary _resourceLibary;

        public GridComponent(ComponentManagerResolver componentManagerResolver, RenderEngineComponent renderEngineComponent, ResourceLibary resourceLibary) 
            : base(componentManagerResolver.ComponentManager)
        {
            _renderEngineComponent = renderEngineComponent;
            _resourceLibary = resourceLibary;
        }

        public override void Initialize()
        {

            _gridMesh = new LineMeshRender(_resourceLibary);
            _gridMesh.CreateGrid();

            base.Initialize();
        }

        public override void Draw(GameTime gameTime)
        {
            _renderEngineComponent.AddRenderItem(RenderBuckedId.Line, new LineRenderItem() { LineMesh = _gridMesh, ModelMatrix = Matrix.Identity });
            base.Draw(gameTime);
        }

        public void Dispose()
        {
            _gridMesh.Dispose();
            _gridMesh = null;
        }
    }
}
