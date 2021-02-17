using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using View3D.Animation;
using View3D.Components.Gizmo;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;

namespace View3D.SceneNodes
{
    public class MeshNode : GroupNode, ITransformable, IDrawableNode, ISelectable, IUpdateable
    {
        public AnimationPlayer AnimationPlayer;
        public Effect DefaultEffect { get; set; }
        public BasicEffect WireframeEffect { get; set; }
        public BasicEffect SelectedFacesEffect { get; set; }

        public int LodIndex { get; set; } = -1;
        public IGeometry Geometry { get; set; }

        public void Update(GameTime time)
        {
            // Animation Handling goes here! 
            /*
              Matrix[] data = new Matrix[256];
            for (int i = 0; i < 256; i++)
                data[i] = Matrix.Identity;
           
            var animatedModel = _model as Rmv2RenderModel;
            if (animatedModel != null)
            {
                _shader.Parameters["WeightCount"].SetValue(animatedModel.WeightCount);

                var player = animatedModel._animationPlayer;
                if (player != null)
                {
                    var frame = player.GetCurrentFrame();
                    if (frame != null)
                    {
                        for (int i = 0; i < frame.BoneTransforms.Count(); i++)
                            data[i] = frame.BoneTransforms[i].WorldTransform;
                    }
                }

               // animatedModel.UpdateVertexBuffer();
            }

           
            _shader.Parameters["tranforms"].SetValue(data);
             */
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            renderEngine.AddRenderItem(RenderBuckedId.Normal, new MeshRenderItem() { World = parentWorld, Node = this });
        }

        public void DrawWireframeOverlay(GraphicsDevice device, Matrix parentWorldMatrix, CommonShaderParameters shaderParams)
        {
            WireframeEffect.Projection = shaderParams.Projection;
            WireframeEffect.View = shaderParams.View;
            WireframeEffect.World = ModelMatrix;
            Geometry.ApplyMesh(WireframeEffect, device);
        }

        public void DrawSelectedFaces(GraphicsDevice device, Matrix parentWorldMatrix, CommonShaderParameters shaderParams, List<int> faces)
        {
            SelectedFacesEffect.Projection = shaderParams.Projection;
            SelectedFacesEffect.View = shaderParams.View;
            SelectedFacesEffect.World = ModelMatrix;
            Geometry.ApplyMeshPart(SelectedFacesEffect, device, faces);
        }

        public void DrawBasic(GraphicsDevice device, Matrix parentWorldMatrix, CommonShaderParameters shaderParams)
        {
            DefaultEffect.Parameters["View"].SetValue(shaderParams.View);
            DefaultEffect.Parameters["Projection"].SetValue(shaderParams.Projection);
            DefaultEffect.Parameters["cameraPosition"].SetValue(shaderParams.CameraPosition);
            DefaultEffect.Parameters["cameraLookAt"].SetValue(shaderParams.CameraLookAt);
            DefaultEffect.Parameters["ViewInverse"].SetValue(Matrix.Invert(shaderParams.View));
            DefaultEffect.Parameters["EnvMapTransform"].SetValue((Matrix.CreateRotationY(shaderParams.EnvRotate)));

            // Pivot stuff
            DefaultEffect.Parameters["World"].SetValue(ModelMatrix * parentWorldMatrix);

            DefaultEffect.Parameters["UseAlpha"].SetValue(false);
            DefaultEffect.Parameters["doAnimation"].SetValue(true);

            //DefaultEffect.Projection = shaderParams.Projection;
            //DefaultEffect.View = shaderParams.View;
            //DefaultEffect.World = ModelMatrix;
            Geometry.ApplyMesh(DefaultEffect, device);
        }

        public override SceneNode Clone()
        {
            var newItem = new MeshNode()
            {
                Geometry = Geometry.Clone(),
                Position = Position,
                Orientation = Orientation,
                Scale = Scale,
                SceneManager = SceneManager,
                IsEditable = IsEditable,
                IsVisible = IsVisible,
                LodIndex = LodIndex,
                Name = Name + " - Clone",
                AnimationPlayer = AnimationPlayer,
            };
            newItem.DefaultEffect = DefaultEffect.Clone();
            newItem.WireframeEffect = (BasicEffect)WireframeEffect.Clone();
            newItem.SelectedFacesEffect = (BasicEffect)SelectedFacesEffect.Clone();
            return newItem;
        }


    }

 
}
