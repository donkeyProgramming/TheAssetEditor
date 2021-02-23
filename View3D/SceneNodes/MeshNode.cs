using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using View3D.Animation;
using View3D.Components.Gizmo;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.Utility;

namespace View3D.SceneNodes
{
    public class MeshNode : GroupNode, ITransformable, IEditableGeometry, ISelectable, IUpdateable, IDrawableItem
    {
        public AnimationPlayer AnimationPlayer;

        private MeshNode()
        { }

        public MeshNode(IGeometry geo, string name, GraphicsDevice device, ResourceLibary resourceLib, AnimationPlayer animationPlayer)
        {
            Geometry = geo;
            AnimationPlayer = animationPlayer;

            Name = name;
            Position = Vector3.Zero;
            Scale = Vector3.One;
            Orientation = Quaternion.Identity;

            DefaultEffect = resourceLib.GetEffect(ShaderTypes.Phazer);

            WireframeEffect = new BasicEffect(device);
            WireframeEffect.DiffuseColor = Vector3.Zero;

            SelectedFacesEffect = new BasicEffect(device);
            SelectedFacesEffect.DiffuseColor = new Vector3(1, 0, 0);
            SelectedFacesEffect.SpecularColor = new Vector3(1, 0, 0);
            SelectedFacesEffect.EnableDefaultLighting();
        }

        public MeshNode(RmvSubModel rmvSubModel, GraphicsDevice device, ResourceLibary resourceLib, AnimationPlayer animationPlayer)
        {
            Geometry = new Rmv2Geometry(rmvSubModel, device);
            AnimationPlayer = animationPlayer;

            Name = rmvSubModel.Header.ModelName;
            Position = Vector3.Zero;
            Scale = Vector3.One;
            Orientation = Quaternion.Identity;

            DefaultEffect = resourceLib.GetEffect(ShaderTypes.Phazer);

            WireframeEffect = new BasicEffect(device);
            WireframeEffect.DiffuseColor = Vector3.Zero;

            SelectedFacesEffect = new BasicEffect(device);
            SelectedFacesEffect.DiffuseColor = new Vector3(1, 0, 0);
            SelectedFacesEffect.SpecularColor = new Vector3(1, 0, 0);
            SelectedFacesEffect.EnableDefaultLighting();

            var diffuse = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Diffuse).Path);
            var specTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Specular).Path);
            var normalTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Normal).Path);
            var glossTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Gloss).Path);

            DefaultEffect.Parameters["DiffuseTexture"].SetValue(diffuse);
            DefaultEffect.Parameters["SpecularTexture"].SetValue(specTexture);
            DefaultEffect.Parameters["NormalTexture"].SetValue(normalTexture);
            DefaultEffect.Parameters["GlossTexture"].SetValue(glossTexture);

            DefaultEffect.Parameters["tex_cube_diffuse"].SetValue(resourceLib.PbrDiffuse);
            DefaultEffect.Parameters["tex_cube_specular"].SetValue(resourceLib.PbrSpecular);
            DefaultEffect.Parameters["specularBRDF_LUT"].SetValue(resourceLib.PbrLut);
        }

        public Effect DefaultEffect { get; set; }
        public BasicEffect WireframeEffect { get; set; }
        public BasicEffect SelectedFacesEffect { get; set; }

        public int LodIndex { get; set; } = -1;
        public IGeometry Geometry { get; set; }

        public void Update(GameTime time)
        {
            // Get animaiton player

            // Animation Handling goes here! 
            
              Matrix[] data = new Matrix[256];
            for (int i = 0; i < 256; i++)
                data[i] = Matrix.Identity;

                DefaultEffect.Parameters["WeightCount"].SetValue(4);// animatedModel.WeightCount);

                var player = AnimationPlayer;
                if (player != null)
                {
                    var frame = player.GetCurrentFrame();
                    if (frame != null)
                    {
                        for (int i = 0; i < frame.BoneTransforms.Count(); i++)
                            data[i] = frame.BoneTransforms[i].WorldTransform;
                    }
                }

   

            DefaultEffect.Parameters["tranforms"].SetValue(data);
             
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            renderEngine.AddRenderItem(RenderBuckedId.Normal, new MeshRenderItem() { Node = this });
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

        public void DrawBasic(GraphicsDevice device, CommonShaderParameters shaderParams)
        {
            DefaultEffect.Parameters["View"].SetValue(shaderParams.View);
            DefaultEffect.Parameters["Projection"].SetValue(shaderParams.Projection);
            DefaultEffect.Parameters["cameraPosition"].SetValue(shaderParams.CameraPosition);
            DefaultEffect.Parameters["cameraLookAt"].SetValue(shaderParams.CameraLookAt);
            DefaultEffect.Parameters["ViewInverse"].SetValue(Matrix.Invert(shaderParams.View));
            DefaultEffect.Parameters["EnvMapTransform"].SetValue((Matrix.CreateRotationY(shaderParams.EnvRotate)));

            // Pivot stuff
            DefaultEffect.Parameters["World"].SetValue(ModelMatrix);

            DefaultEffect.Parameters["UseAlpha"].SetValue(false);
            DefaultEffect.Parameters["doAnimation"].SetValue(AnimationPlayer.IsEnabled);

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
            newItem.DefaultEffect.Parameters["DiffuseTexture"].SetValue(DefaultEffect.Parameters["DiffuseTexture"].GetValueTexture2D());
            newItem.DefaultEffect.Parameters["SpecularTexture"].SetValue(DefaultEffect.Parameters["SpecularTexture"].GetValueTexture2D());
            newItem.DefaultEffect.Parameters["NormalTexture"].SetValue(DefaultEffect.Parameters["NormalTexture"].GetValueTexture2D());
            newItem.DefaultEffect.Parameters["GlossTexture"].SetValue(DefaultEffect.Parameters["GlossTexture"].GetValueTexture2D());

            newItem.DefaultEffect.Parameters["tex_cube_diffuse"].SetValue(DefaultEffect.Parameters["tex_cube_diffuse"].GetValueTextureCube());
            newItem.DefaultEffect.Parameters["tex_cube_specular"].SetValue(DefaultEffect.Parameters["tex_cube_specular"].GetValueTextureCube());
            newItem.DefaultEffect.Parameters["specularBRDF_LUT"].SetValue(DefaultEffect.Parameters["specularBRDF_LUT"].GetValueTexture2D());

            newItem.WireframeEffect = (BasicEffect)WireframeEffect.Clone();
            newItem.SelectedFacesEffect = (BasicEffect)SelectedFacesEffect.Clone();
            return newItem;
        }
    }
}
