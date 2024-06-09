﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using View3D.Components.Component.Selection;
using View3D.Components.Rendering;
using View3D.SceneNodes;

namespace View3D.Rendering.RenderItems
{
    public class VertexRenderItem : IRenderItem
    {
        public VertexInstanceMesh VertexRenderer { get; set; }

        public Rmv2MeshNode Node { get; set; }
        public Matrix ModelMatrix { get; set; } = Matrix.Identity;
        public VertexSelectionState SelectedVertices { get; set; }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            VertexRenderer.Update(Node.Geometry, Node.RenderMatrix, Node.Orientation, parameters.CameraPosition, SelectedVertices);
            VertexRenderer.Draw(parameters.View, parameters.Projection, device, new Vector3(0, 1, 0));
        }
    }
}
